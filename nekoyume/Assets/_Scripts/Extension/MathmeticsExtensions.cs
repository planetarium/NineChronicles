using Unity.Mathematics;

namespace Nekoyume.Extension
{
    public static class MathematicsExtensions
    {
        public static float2 ReverseX(this float2 value)
        {
            return new float2(-value.x, value.y);
        }
    }
}
