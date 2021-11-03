namespace Nekoyume.Extensions
{
    public static class IntegerExtensions
    {
        public static decimal NormalizeFromTenThousandths(this int value) => value * 0.0001m;
    }
}
