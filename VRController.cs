using Common;
using FezGame;
using Microsoft.Xna.Framework;
using Valve.VR;

namespace HEADSET
{
    public class VRController : GameComponent
    {
        public static VRController Instance { get; private set; }

        private TweaksCollection tweaks = new();
        private bool openVrInitialized = false;

        TrackedDevicePose_t[] renderPoses;
        TrackedDevicePose_t[] gamePoses;

        public Fez Fez { get; private set; }

        public static bool OpenVRReady => OpenVR.IsHmdPresent() && OpenVR.IsRuntimeInstalled();
        public static bool OpenVRActive => OpenVRReady && OpenVR.Compositor != null && OpenVR.System != null;
        public static bool Active => Instance.openVrInitialized;

        public VRController(Game game) : base(game)
        {
            Instance = this;

            Fez = (Fez)game;
            Enabled = true;

            renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        }

        public override void Initialize()
        {
            base.Initialize();
            TryBeginSession();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (openVrInitialized && !OpenVRActive)
            {
                EndSession();
            }
        }

        public bool TryBeginSession()
        {
            if (openVrInitialized)
            {
                return true;
            }

            var status = TryInitializeDevice();
            if (status != EVRInitError.None)
            {
                Logger.Log("HEADSET", $"VR device failed to initialize. Error: {status}");
                return false;
            }

            OpenVR.Compositor.FadeGrid(0.5f, false);
            OpenVR.Compositor.FadeToColor(0.5f, 0, 0, 0, 0, false);

            tweaks.Initialize();

            Logger.Log("HEADSET", $"VR session is active.");

            return true;
        }

        public void PrepareForNextFrame()
        {
            OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
        }

        public void EndSession()
        {
            if (!openVrInitialized)
            {
                return;
            }

            tweaks.Dispose();
            DeinitializeDevice();

            Logger.Log("HEADSET", $"VR session has ended.");
        }

        private EVRInitError TryInitializeDevice()
        {
            if (!OpenVRReady)
            {
                return EVRInitError.Init_HmdNotFound;
            }

            EVRInitError initError = EVRInitError.None;
            OpenVR.Init(ref initError, EVRApplicationType.VRApplication_Scene);

            if (initError != EVRInitError.None || !OpenVRActive)
            {
                OpenVR.Shutdown();
                return initError;
            }

            openVrInitialized = true;
            return EVRInitError.None;
        }

        private void DeinitializeDevice()
        {
            if (!openVrInitialized)
            {
                return;
            }

            OpenVR.Shutdown();
            openVrInitialized = false;
        }

        protected override void Dispose(bool disposing)
        {
            EndSession();
            base.Dispose(disposing);
        }
    }
}
