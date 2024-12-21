using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace HEADSET.Tweaks
{
    internal abstract class DrawableComponentTweak<T> : IVRTweak where T : DrawableGameComponent
    {
        private IDetour updateCallDetour;
        private IDetour drawCallDetour;

        private T currentComponent;

        private Action<T, GameTime> originalUpdateCall;
        private Action<T, GameTime> originalDrawCall;

        protected T SelfComponent => currentComponent;

        public void Initialize()
        {
            var updateCall = typeof(T).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            updateCallDetour = new Hook(updateCall, UpdateHook);

            var drawCall = typeof(T).GetMethod("Draw", BindingFlags.Public | BindingFlags.Instance);
            drawCallDetour = new Hook(drawCall, DrawHook);

            OnInitialize();
        }

        void UpdateHook(Action<T, GameTime> original, T component, GameTime gameTime)
        {
            originalUpdateCall = original;
            currentComponent = component;
            Update(gameTime);
        }

        void DrawHook(Action<T, GameTime> original, T component, GameTime gameTime)
        {
            originalDrawCall = original;
            currentComponent = component;
            Draw(gameTime);
        }

        protected virtual void Update(GameTime gameTime)
        {
            if (currentComponent != null && originalUpdateCall != null)
            {
                originalUpdateCall(currentComponent, gameTime);
            }
        }
        protected virtual void Draw(GameTime gameTime)
        {
            if (currentComponent != null && originalDrawCall != null)
            {
                originalDrawCall(currentComponent, gameTime);
            }
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnDispose() { }

        public void Dispose()
        {
            OnDispose();
            updateCallDetour.Dispose();
            drawCallDetour.Dispose();
        }
    }
}
