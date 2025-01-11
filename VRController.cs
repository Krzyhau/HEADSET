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

        public Fez Fez { get; private set; }

        public static bool OpenVRActive => OpenVR.System != null && OpenVR.IsHmdPresent();
        public static bool Active => OpenVRActive && Instance.openVrInitialized;

        public VRController(Game game) : base(game)
        {
            Instance = this;

            Fez = (Fez)game;
            Enabled = true;
        }

        public override void Initialize()
        {
            base.Initialize();
            tweaks.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            EnsureProperOpenVRState();
        }

        private void EnsureProperOpenVRState()
        {
            bool openVrActive = OpenVR.System != null && OpenVR.IsHmdPresent();

            if (!openVrActive && openVrInitialized)
            {
                DeinitializeOpenVR();
            }

            else if(!openVrActive && !openVrInitialized)
            {
                TryInitializeOpenVR();
            }
        }

        private void TryInitializeOpenVR()
        {
            if (openVrInitialized)
            {
                return;
            }

            EVRInitError initError = EVRInitError.None;
            OpenVR.Init(ref initError, EVRApplicationType.VRApplication_Scene);

            if (initError != EVRInitError.None || OpenVR.Compositor == null)
            {
                OpenVR.Shutdown();
            }

            openVrInitialized = true;
            Logger.Log("HEADSET", $"OpenVR initialized.");
        }

        private void DeinitializeOpenVR()
        {
            OpenVR.Shutdown();
            openVrInitialized = false;
            Logger.Log("HEADSET", $"OpenVR has been shut down.");
        }

        protected override void Dispose(bool disposing)
        {
            tweaks.Dispose();
            base.Dispose(disposing);
            OpenVR.Shutdown();
        }
    }
}
