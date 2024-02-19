using System;
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

        public static int ConvertToInt32(string value)
        {
            int result = 0;
            try
            {
                result = Convert.ToInt32(value);
            }
            catch (FormatException ex)
            {
                Console.WriteLine("FormatException: " + ex.Message);
                return result;
            }
            catch (OverflowException ex)
            {
                if (value.StartsWith("-"))
                {
                    result = int.MinValue;
                }
                else
                {
                    result = int.MaxValue;
                }
                Console.WriteLine("OverflowException: " + ex.Message);
            }
            return result;
        }
    }
}
