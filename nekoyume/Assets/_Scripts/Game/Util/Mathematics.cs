using Unity.Mathematics;

namespace Nekoyume.Game.Util
{
    public readonly struct Float2
    {
        public static readonly float2 ZeroZero = new float2(0f, 0f);
        public static readonly float2 ZeroHalf = new float2(0f, 0.5f);
        public static readonly float2 ZeroOne = new float2(0f, 1f);
        public static readonly float2 HalfZero = new float2(0.5f, 0f);
        public static readonly float2 HalfHalf = new float2(0.5f, 0.5f);
        public static readonly float2 HalfOne = new float2(0.5f, 1f);
        public static readonly float2 OneZero = new float2(1f, 0f);
        public static readonly float2 OneHalf = new float2(1f, 0.5f);
        public static readonly float2 OneOne = new float2(1f, 1f);
    }
}
