using Common;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace HEADSET
{
    public class VRMainController : GameComponent
    {
        public static Fez Fez { get; private set; }

        public VRMainController(Game game) : base(game)
        {
            Fez = (Fez)game;
            Enabled = true;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
