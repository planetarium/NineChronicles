using Unity.Mathematics;

namespace Nekoyume.Game.Util
{
    public readonly struct Float2
    {
        public static readonly float2 ZeroZero = new(0f, 0f);
        public static readonly float2 ZeroHalf = new(0f, 0.5f);
        public static readonly float2 ZeroOne = new(0f, 1f);
        public static readonly float2 HalfZero = new(0.5f, 0f);
        public static readonly float2 HalfHalf = new(0.5f, 0.5f);
        public static readonly float2 HalfOne = new(0.5f, 1f);
        public static readonly float2 OneZero = new(1f, 0f);
        public static readonly float2 OneHalf = new(1f, 0.5f);
        public static readonly float2 OneOne = new(1f, 1f);
    }
}
