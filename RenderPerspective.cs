using Valve.VR;

namespace HEADSET
{
    public enum RenderPerspective
    {
        Default,
        LeftEye,
        RightEye,
    }

    public static class RenderPrespectiveExtensions
    {
        public static EVREye ToVREye(this RenderPerspective perspective)
        {
            return perspective switch
            {
                RenderPerspective.LeftEye => EVREye.Eye_Left,
                RenderPerspective.RightEye => EVREye.Eye_Right,
                _ => EVREye.Eye_Left,
            };
        }
    }
}
