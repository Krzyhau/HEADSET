using FezEngine;
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
            var drawCallback = () => original(self, originalGameTime);
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

        public void Dispose()
        {
            TargetRenderer.ReturnTarget(leftEyeRenderHandle);
            TargetRenderer.ReturnTarget(rightEyeRenderHandle);

            mainGameDrawLoopDetour.Dispose();
        }
    }
}
