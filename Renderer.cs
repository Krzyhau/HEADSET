﻿using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using HEADSET.Tweaks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Valve.VR;

namespace HEADSET
{
    internal class Renderer : IVRTweak
    {
        public static Renderer Instance { get; private set; }
        public static RenderPerspective Perspective { get; private set; }

        private Mesh previewPlane;
        private IDetour mainGameDrawLoopDetour;
        private RenderTargetHandle leftEyeRenderHandle;
        private RenderTargetHandle rightEyeRenderHandle;

        private GraphicsDevice GraphicsDevice => GraphicsDeviceService.GraphicsDevice;
        [ServiceDependency] public IGraphicsDeviceService GraphicsDeviceService { private get; set; }
        [ServiceDependency] public ITargetRenderingManager TargetRenderer { private get; set; }

        public Renderer()
        {
            Instance = this;
            Perspective = RenderPerspective.Default;
        }

        public void Initialize()
        {
            InjectHooks();

            leftEyeRenderHandle = TargetRenderer.TakeTarget();
            rightEyeRenderHandle = TargetRenderer.TakeTarget();

            DrawActionScheduler.Schedule(CreateEyeRenderPlane);
        }

        private void CreateEyeRenderPlane()
        {
            previewPlane = new Mesh
            {
                DepthWrites = false,
                AlwaysOnTop = true,
            };
            previewPlane.AddFace(Vector3.One * 2, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);

            previewPlane.Effect = new BasicPostEffect()
            {
                ForcedViewMatrix = Matrix.Identity,
                ForcedProjectionMatrix = Matrix.Identity,
                IgnoreCache = true
            };
        }

        private void InjectHooks()
        {
            var drawMethod = typeof(Fez).GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Instance);
            mainGameDrawLoopDetour = new Hook(drawMethod, DrawHook);
        }

        private void DrawHook(Action<Fez, GameTime> original, Fez self, GameTime originalGameTime)
        {
            var gameTimeForLaterDrawCalls = new GameTime(
                originalGameTime.TotalGameTime,
                new TimeSpan(0)
            );
            bool firstDrawCallDone = false;
            var drawCallback = () =>
            {
                var gameTime = firstDrawCallDone ? gameTimeForLaterDrawCalls : originalGameTime;
                original(self, gameTime);
                firstDrawCallDone = true;
            };
            Render(drawCallback);
        }

        void Render(Action drawCallback)
        {
            if (!VRController.Active)
            {
                Perspective = RenderPerspective.Default;
                drawCallback();
                return;
            }

            DrawToEyeTexture(RenderPerspective.LeftEye, drawCallback);
            DrawToEyeTexture(RenderPerspective.RightEye, drawCallback);

            DrawEyePreviewOnScreen(RenderPerspective.LeftEye);
        }

        private void DrawToEyeTexture(RenderPerspective perspective, Action drawCallback)
        {
            Perspective = perspective;

            var eyeRenderTarget = GetRenderTargetForEye(perspective);
            TargetRenderer.ScheduleHook(9999999, eyeRenderTarget);
            drawCallback();
            TargetRenderer.Resolve(eyeRenderTarget, reschedule: false);
        }

        private void DrawEyePreviewOnScreen(RenderPerspective perspective)
        {
            if (previewPlane == null || perspective == RenderPerspective.Default)
            {
                return;
            }

            var eyeRenderTarget = GetRenderTargetForEye(perspective);

            GraphicsDevice.SetupViewport();
            GraphicsDevice.PrepareDraw();
            GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);

            previewPlane.Texture.Set(eyeRenderTarget);
            previewPlane.Draw();
        }

        private RenderTarget2D GetRenderTargetForEye(RenderPerspective perspective)
        {
            return perspective switch
            {
                RenderPerspective.LeftEye => leftEyeRenderHandle?.Target,
                RenderPerspective.RightEye => rightEyeRenderHandle?.Target,
                _ => null
            };
        }

        private void SubmitTexturesToOpenVR()
        {
            var eyeBounds = new VRTextureBounds_t {
                uMin = 0,
                uMax = 1,
                vMin = 1,
                vMax = 0
            };

            var leftEyeTexture = GetVRTextureForEye(RenderPerspective.LeftEye);
            var rightEyeTexture = GetVRTextureForEye(RenderPerspective.RightEye);

            OpenVR.Compositor.Submit(EVREye.Eye_Left, ref leftEyeTexture, ref eyeBounds, EVRSubmitFlags.Submit_Default);
            OpenVR.Compositor.Submit(EVREye.Eye_Right, ref rightEyeTexture, ref eyeBounds, EVRSubmitFlags.Submit_Default);
        }

        private Texture_t GetVRTextureForEye(RenderPerspective perspective)
        {
            return new Texture_t
            {
                handle = GetTextureHandleForEye(perspective),
                eType = ETextureType.OpenGL,
                eColorSpace = EColorSpace.Auto,
            };
        }

        private IntPtr GetTextureHandleForEye(RenderPerspective perspective)
        {
            var renderTarget = GetRenderTargetForEye(RenderPerspective.LeftEye);

            var textureProperty = typeof(Texture).GetField("texture", BindingFlags.NonPublic | BindingFlags.Instance);
            var texture = textureProperty.GetValue(renderTarget);

            if (texture == null)
            {
                return IntPtr.Zero;
            }

            var handleProperty = texture.GetType().GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);
            uint textureHandle = (uint)handleProperty.GetValue(texture);

            return (IntPtr)textureHandle;
        }

        public void Dispose()
        {
            TargetRenderer.ReturnTarget(leftEyeRenderHandle);
            TargetRenderer.ReturnTarget(rightEyeRenderHandle);

            mainGameDrawLoopDetour.Dispose();
        }
    }
}
