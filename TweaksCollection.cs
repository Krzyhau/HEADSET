using FezEngine.Tools;
using HEADSET.Tweaks;
using System.Reflection;

namespace HEADSET
{
    internal class TweaksCollection
    {
        private readonly List<IVRTweak> tweaks = new();

        private void PopulateTweakList()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && typeof(IVRTweak).IsAssignableFrom(t)))
            {
                IVRTweak tweak = (IVRTweak)Activator.CreateInstance(type);
                tweaks.Add(tweak);
            }
        }

        public void Initialize()
        {
            PopulateTweakList();

            foreach (var tweak in tweaks)
            {
                ServiceHelper.InjectServices(tweak);
                tweak.Initialize();
            }
        }

        public void Dispose()
        {
            foreach (var tweak in tweaks)
            {
                tweak.Dispose();
            }

            tweaks.Clear();
        }
    }
}
