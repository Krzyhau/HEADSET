using FezEngine.Services;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Valve.VR;

namespace HEADSET.Tweaks
{
    internal class GameCameraTweak : IVRTweak
    {
        private IDetour cameraInterpolationDetour;

        private MethodInfo cameraInterpolationMethod;
        private FieldInfo cameraProjectionMatrixField;
        private MethodInfo cameraOnProjectionChangedMethod;

        public void Initialize()
        {
            CollectReflections();
            InjectIntoCamera();
        }

        private void CollectReflections()
        {
            cameraInterpolationMethod = typeof(DefaultCameraManager).GetMethod("InterpolationCallback", BindingFlags.Public | BindingFlags.Instance);
            cameraProjectionMatrixField = typeof(CameraManager).GetField("projection", BindingFlags.NonPublic | BindingFlags.Instance);
            cameraOnProjectionChangedMethod = typeof(DefaultCameraManager).GetMethod("OnProjectionChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void InjectIntoCamera()
        {
            cameraInterpolationDetour = new Hook(cameraInterpolationMethod, CameraInterpolationCallbackHook);
        }

        private void CameraInterpolationCallbackHook(Action<DefaultCameraManager, GameTime> original, DefaultCameraManager self, GameTime gameTime)
        {
            original(self, gameTime);

            Matrix projMatrix = GetProjectionMatrixForPerspective(Renderer.Perspective);
            cameraProjectionMatrixField.SetValue(self, projMatrix);

            cameraOnProjectionChangedMethod.Invoke(self, new object[] {});
        }

        private Matrix GetProjectionMatrixForPerspective(RenderPerspective perspective)
        {
            TrackedDevicePose_t hmdPose = new();
            TrackedDevicePose_t hmdGamePose = new();
            OpenVR.Compositor.GetLastPoseForTrackedDeviceIndex(OpenVR.k_unTrackedDeviceIndex_Hmd, ref hmdPose, ref hmdGamePose);

            var eye = perspective.ToVREye();

            var gamePoseMatrix = Hmd34ToXnaMatrix(hmdPose.mDeviceToAbsoluteTracking);
            var eyeToHeadMatrix = Hmd34ToXnaMatrix(OpenVR.System.GetEyeToHeadTransform(eye));
            var projectionMatrix = Hmd44ToXnaMatrix(OpenVR.System.GetProjectionMatrix(eye, 0.001f, 1000.0f));

            return Matrix.Invert(gamePoseMatrix) * eyeToHeadMatrix * projectionMatrix;
        }

        private Matrix Hmd44ToXnaMatrix(HmdMatrix44_t hmdMatrix) {
            return new Matrix(
                hmdMatrix.m0, hmdMatrix.m4, hmdMatrix.m8, hmdMatrix.m12,
                hmdMatrix.m1, hmdMatrix.m5, hmdMatrix.m9, hmdMatrix.m13,
                hmdMatrix.m2, hmdMatrix.m6, hmdMatrix.m10, hmdMatrix.m14,
                hmdMatrix.m3, hmdMatrix.m7, hmdMatrix.m11, hmdMatrix.m15
            );
        }

        private Matrix Hmd34ToXnaMatrix(HmdMatrix34_t hmdMatrix)
        {
            return new Matrix(
                hmdMatrix.m0, hmdMatrix.m4, hmdMatrix.m8, 0,
                hmdMatrix.m1, hmdMatrix.m5, hmdMatrix.m9, 0,
                hmdMatrix.m2, hmdMatrix.m6, hmdMatrix.m10, 0,
                hmdMatrix.m3, hmdMatrix.m7, hmdMatrix.m11, 1
            );
        }

        public void Dispose()
        {
            cameraInterpolationDetour.Dispose();
        }
    }
}
