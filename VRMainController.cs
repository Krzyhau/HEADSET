using FezGame;
using Microsoft.Xna.Framework;

namespace HEADSET
{
    public class VRMainController : GameComponent
    {
        private Renderer renderer;

        public Fez Fez { get; private set; }

        public VRMainController(Game game) : base(game)
        {
            Fez = (Fez)game;
            Enabled = true;
        }

        public override void Initialize()
        {
            base.Initialize();

            renderer = new Renderer(this);
        }

        protected override void Dispose(bool disposing)
        {
            renderer.Dispose();
            base.Dispose(disposing);
        }
    }
}
