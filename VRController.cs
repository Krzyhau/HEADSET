using FezEngine.Tools;
using FezGame;
using HEADSET.Tweaks;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace HEADSET
{
    public class VRController : GameComponent
    {
        public static VRController Instance { get; private set; }
        public static bool Active { get; private set; }

        private TweaksCollection tweaks = new();

        public Fez Fez { get; private set; }

        public VRController(Game game) : base(game)
        {
            Instance = this;
            Active = true;

            Fez = (Fez)game;
            Enabled = true;
        }

        public override void Initialize()
        {
            base.Initialize();
            tweaks.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            tweaks.Dispose();
            base.Dispose(disposing);
        }
    }
}
