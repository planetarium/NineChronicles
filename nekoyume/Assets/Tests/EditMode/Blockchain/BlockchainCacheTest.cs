using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Nekoyume.Blockchain;
using NUnit.Framework;

namespace Tests.EditMode.Blockchain
{
    public class BlockchainCacheTest
    {
        private Currency _currency1;
        private Currency _currency2;

        [SetUp]
        public void SetUp()
        {
            _currency1 = Currency.Capped("C1", 0, (100, 0), minters: null);
            _currency2 = Currency.Capped("C2", 0, (100, 0), minters: null);
        }

        [Test]
        public void Add()
        {
            var cache = new BlockchainCache(3);
            var addr = new PrivateKey().ToAddress();
            cache.Add(addr, _currency1 * 100, 0);
            cache.Add(addr, _currency1 * 100, 1);
            cache.Add(addr, _currency1 * 100, 2);
            cache.Add(addr, _currency2 * 100, 2);
        }

        [Test]
        public void TryGetBalance()
        {
            var cache = new BlockchainCache(1);
            var addr = new PrivateKey().ToAddress();
            Assert.IsFalse(cache.TryGetBalance(addr, _currency1, out _));
            cache.Add(addr, _currency1 * 100, blockIndex: 0);
            Assert.IsTrue(cache.TryGetBalance(addr, _currency1,
                out var outBalance,
                out var outBlockIndex,
                out var outBlockHash,
                out var outStateRootHash));

            Assert.IsTrue(cache.TryGetBalance(0, addr, _currency1,
                out outBalance,
                out outBlockHash,
                out outStateRootHash));
            Assert.AreEqual(_currency1 * 100, outBalance);
            Assert.IsNull(outBlockHash);
            Assert.IsNull(outStateRootHash);

            var blockHash = BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(addr, _currency1 * 100, blockHash: blockHash);
            Assert.IsTrue(cache.TryGetBalance(blockHash, addr, _currency1,
                out outBalance,
                out outBlockIndex,
                out outStateRootHash));
            Assert.AreEqual(_currency1 * 100, outBalance);
            Assert.IsNull(outBlockIndex);
            Assert.IsNull(outStateRootHash);

            var stateRootHash =
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(addr, _currency1 * 100, stateRootHash: stateRootHash);
            Assert.IsTrue(cache.TryGetBalance(stateRootHash, addr, _currency1,
                out outBalance,
                out outBlockIndex,
                out outBlockHash));
            Assert.AreEqual(_currency1 * 100, outBalance);
            Assert.IsNull(outBlockIndex);
            Assert.IsNull(outBlockHash);

            Assert.IsFalse(cache.TryGetBalance(addr, _currency2, out _));
            cache.Add(addr, _currency2 * 100, blockIndex: 1);
            Assert.IsFalse(cache.TryGetBalance(addr, _currency1, out _));
            Assert.IsTrue(cache.TryGetBalance(addr, _currency2,
                out outBalance,
                out outBlockIndex,
                out outBlockHash,
                out outStateRootHash));
            Assert.AreEqual(_currency2 * 100, outBalance);
            Assert.AreEqual(1, outBlockIndex);
            Assert.IsNull(outBlockHash);
            Assert.IsNull(outStateRootHash);
        }

        [Test]
        public void ClearBalance()
        {
            var cache = new BlockchainCache(1);
            var addr = new PrivateKey().ToAddress();
            cache.Add(addr, _currency1 * 100, 0);
            cache.ClearBalance();
            Assert.IsFalse(cache.TryGetBalance(addr, _currency1, out _));
        }
    }
}
