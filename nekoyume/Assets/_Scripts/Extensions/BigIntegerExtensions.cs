using System;
using System.Numerics;

namespace Nekoyume
{
    public static class BigIntegerExtensions
    {
        public static string ToCurrencyNotation(this BigInteger num)
        {
            var absoluteValue = BigInteger.Abs(num);
            var exponent = BigInteger.Log10(absoluteValue);
            if (absoluteValue >= BigInteger.One)
            {
                switch ((long) Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        return num.ToString();
                    case 4:
                    case 5:
                    case 6:
                        return BigInteger.Divide(num, (BigInteger)1e3) + "K";
                    case 7:
                    case 8:
                    case 9:
                        return BigInteger.Divide(num, (BigInteger)1e6) + "M";
                    default:
                        return BigInteger.Divide(num, (BigInteger)1e9) + "B";
                }
            }

            return num.ToString();
        }

        public static string ToCurrencyNotation(this int num)
        {
            var absoluteValue = BigInteger.Abs(num);
            var exponent = BigInteger.Log10(absoluteValue);
            if (absoluteValue >= BigInteger.One)
            {
                switch ((long) Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        return num.ToString();
                    case 4:
                    case 5:
                    case 6:
                        return BigInteger.Divide(num, (BigInteger)1e3) + "K";
                    case 7:
                    case 8:
                    case 9:
                        return BigInteger.Divide(num, (BigInteger)1e6) + "M";
                    default:
                        return BigInteger.Divide(num, (BigInteger)1e9) + "B";
                }
            }

            return num.ToString();
        }
    }
}
