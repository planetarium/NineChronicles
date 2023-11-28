using Libplanet.Common;
using Libplanet.Crypto;

namespace Nekoyume
{
    public static class PrivateKeyExtensions
    {
        public static string ToHexWithZeroPaddings(this PrivateKey privateKey)
        {
            var hex = ByteUtil.Hex(privateKey.ByteArray);
            return hex.PadLeft(64, '0');
        }
    }
}
