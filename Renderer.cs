using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace HEADSET
{
    internal class Renderer
    {
        public static Renderer Instance { get; private set; }
        public static RenderPerspective Perspective { get; private set; }

        private Mesh previewPlane;

        private IDetour mainGameDrawLoopDetour;

        private RenderTargetHandle leftEyeRenderHandle;
        private RenderTargetHandle rightEyeRenderHandle;

        public VRMainController Controller { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ITargetRenderingManager TargetRenderer { get; private set; }

        public Renderer(VRMainController controller)
        {
            Instance = this;
            Perspective = RenderPerspective.Default;

            Controller = controller;
            GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
            TargetRenderer = ServiceHelper.Get<ITargetRenderingManager>();

            InjectHooks();

            leftEyeRenderHandle = TargetRenderer.TakeTarget();
            rightEyeRenderHandle = TargetRenderer.TakeTarget();

            DrawActionScheduler.Schedule(CreateEyeRenderPlane);
        }

        private void CreateEyeRenderPlane()
        {
            previewPlane  = new Mesh
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
            DrawToEyeTexture(RenderPerspective.LeftEye, drawCallback);
            DrawToEyeTexture(RenderPerspective.RightEye, drawCallback);

            DrawEyeOnScreen(RenderPerspective.LeftEye);
        }

        private void DrawToEyeTexture(RenderPerspective perspective, Action drawCallback)
        {
            Perspective = perspective;

            var eyeRenderTarget = GetRenderTargetForEye(perspective);
            TargetRenderer.ScheduleHook(9999999, eyeRenderTarget);
            drawCallback();
            TargetRenderer.Resolve(eyeRenderTarget, reschedule: false);
        }

        private void DrawEyeOnScreen(RenderPerspective perspective)
        {
            if(previewPlane == null)
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
                RenderPerspective.LeftEye => leftEyeRenderHandle.Target,
                RenderPerspective.RightEye => rightEyeRenderHandle.Target,
                _ => throw new NotImplementedException(),
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
