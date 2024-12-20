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
        const int EyeDrawOrder = 9999999;

        private Mesh leftEyePlane;
        private Mesh rightEyePlane;

        private IDetour mainGameDrawLoopDetour;

        private RenderTargetHandle leftEyeRenderHandle;
        private RenderTargetHandle rightEyeRenderHandle;

        public VRMainController Controller { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ITargetRenderingManager TargetRenderer { get; private set; }

        public RenderPerspective CurrentPerspective { get; private set; }

        public Renderer(VRMainController controller)
        {
            Controller = controller;
            GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
            TargetRenderer = ServiceHelper.Get<ITargetRenderingManager>();

            Inject();

            leftEyeRenderHandle = TargetRenderer.TakeTarget();
            rightEyeRenderHandle = TargetRenderer.TakeTarget();

            DrawActionScheduler.Schedule(delegate
            {
                leftEyePlane = CreateEyeRenderPlane(Vector3.Left * 0.5f);
                rightEyePlane = CreateEyeRenderPlane(Vector3.Right * 0.5f);
            });
        }

        private Mesh CreateEyeRenderPlane(Vector3 position)
        {
            var planeMesh = new Mesh
            {
                DepthWrites = false,
                AlwaysOnTop = true,
            };
            planeMesh.AddFace(Vector3.One + Vector3.Up, position, FaceOrientation.Front, centeredOnOrigin: true);

            planeMesh.Effect = new BasicPostEffect()
            {
                ForcedViewMatrix = Matrix.Identity,
                ForcedProjectionMatrix = Matrix.Identity,
                IgnoreCache = true
            };

            return planeMesh;
        }

        private void Inject()
        {
            var drawMethod = typeof(Fez).GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Instance);
            mainGameDrawLoopDetour = new Hook(drawMethod, DrawHook);
        }

        private void DrawHook(Action<Fez, GameTime> original, Fez self, GameTime originalGameTime)
        {
            var drawCallback = (GameTime gameTime) => original(self, gameTime);
            Render(new SplitTimeDrawCallbackProvider(drawCallback, originalGameTime));
        }

        void Render(SplitTimeDrawCallbackProvider callbackProvider)
        {
            var mainDrawCallback = callbackProvider.RequestCallback();
            var leftEyeCallback = callbackProvider.RequestCallback();
            var rightEyeCallback = callbackProvider.RequestCallback();

            DrawToEyeTexture(RenderPerspective.LeftEye, leftEyeCallback);
            DrawToEyeTexture(RenderPerspective.RightEye, rightEyeCallback);

            DrawEyeOnScreen(RenderPerspective.LeftEye);
            DrawEyeOnScreen(RenderPerspective.RightEye);
        }

        private void DrawToEyeTexture(RenderPerspective perspective, Action drawCallback)
        {
            var eyeRenderTarget = GetRenderTargetForEye(perspective);

            TargetRenderer.ScheduleHook(EyeDrawOrder, eyeRenderTarget);
            CurrentPerspective = perspective;
            drawCallback();
            TargetRenderer.Resolve(eyeRenderTarget, reschedule: false);
        }

        private void DrawEyeOnScreen(RenderPerspective perspective)
        {
            if(leftEyePlane == null || rightEyePlane == null)
            {
                return;
            }

            var eyeRenderTarget = GetRenderTargetForEye(perspective);

            GraphicsDevice.SetupViewport();
            GraphicsDevice.PrepareDraw();
            GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);

            var plane = perspective switch
            {
                RenderPerspective.LeftEye => leftEyePlane,
                RenderPerspective.RightEye => rightEyePlane,
                _ => throw new System.NotImplementedException(),
            };

            plane.Texture.Set(eyeRenderTarget);
            plane.Draw();
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
