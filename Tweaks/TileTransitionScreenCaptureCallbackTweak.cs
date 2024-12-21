using FezGame.Components;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace HEADSET.Tweaks
{
    /// <summary>
    /// Fixes the issue with tile transitions executing the ScreenCaptured callback before the game
    /// can pass through update loop as a result of calling draw loop multiple times for each eye.
    /// This has lead to unintentional behaviour (such as Intro not being deinitialized properly).
    ///
    /// In theory, this should also happen in vanilla game with higher framerates, but I wasn't able
    /// to reproduce it, so something fishy is going on there that I am yet to understand.
    /// </summary>
    internal class TileTransitionScreenCaptureCallbackTweak : DrawableComponentTweak<TileTransition>
    {
        private readonly PropertyInfo ScreenCapturedProperty;

        private bool hasBeenUpdated;
        private Action deferredScreenCaptureCallback;

        public TileTransitionScreenCaptureCallbackTweak()
        {
            ScreenCapturedProperty = typeof(TileTransition).GetProperty(
                nameof(TileTransition.ScreenCaptured), 
                BindingFlags.Public | BindingFlags.Instance);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            hasBeenUpdated = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            TryDeferScreenCapture();
            TryCallDeferredScreenCapture();
            base.Draw(gameTime);
        }

        private void TryDeferScreenCapture()
        {
            if (deferredScreenCaptureCallback != null)
            {
                return;
            }

            var screenCaptureCallback = ScreenCapturedProperty.GetValue(SelfComponent) as Action;
            if(screenCaptureCallback != null)
            {
                deferredScreenCaptureCallback = screenCaptureCallback;
                ScreenCapturedProperty.SetValue(SelfComponent, () => { });
                hasBeenUpdated = false;
            }
        }

        private void TryCallDeferredScreenCapture()
        {
            if (!hasBeenUpdated)
            {
                return;
            }

            if (deferredScreenCaptureCallback != null)
            {
                deferredScreenCaptureCallback();
                deferredScreenCaptureCallback = null;
            }
        }
    }
}
