using System;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume;
using NUnit.Framework;

namespace Tests.EditMode.Extensions
{
    public class PrivateKeyExtensionsTest
    {
        [Test]
        public void ToHexWithZeroPaddings()
        {
            const string hexWithZeroPaddings =
                "00000102030405060708090a0102030405060708090b0102030405060708090c";
            var privateKey = new PrivateKey(hexWithZeroPaddings);
            Assert.AreNotEqual(hexWithZeroPaddings, ByteUtil.Hex(privateKey.ByteArray));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrivateKey(ByteUtil.Hex(privateKey.ByteArray)));
            Assert.AreEqual(hexWithZeroPaddings, privateKey.ToHexWithZeroPaddings());
            Assert.DoesNotThrow(() => new PrivateKey(privateKey.ToHexWithZeroPaddings()));
        }
    }
}
