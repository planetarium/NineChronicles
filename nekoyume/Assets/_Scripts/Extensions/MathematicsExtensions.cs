using Unity.Mathematics;

namespace Nekoyume
{
    public static class MathematicsExtensions
    {
        public static float2 ReverseX(this float2 value)
        {
            return new float2(-value.x, value.y);
        }

        public static float2 ReverseY(this float2 value)
        {
            return new float2(value.x, -value.y);
        }

        public static float2 Reverse(this float2 value)
        {
            return new float2(-value.x, -value.y);
        }
    }
}
