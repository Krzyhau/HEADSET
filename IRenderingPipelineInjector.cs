using FezGame;

namespace HEADSET
{
    internal interface IRenderingPipelineInjector
    {
        void Inject(Fez fez);
        void Discard();
        void Preconfigure(Renderer renderer);
    }
}
