using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Types.Blocks;
using Nekoyume.Blockchain;
using NUnit.Framework;

namespace Tests.EditMode.Blockchain
{
    public class BlockHashCacheTest
    {
        [Test]
        public void Add()
        {
            var cache = new BlockHashCache(2);
            var blockHash = BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash =
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(0, blockHash, stateRootHash);
            cache.Add(1, blockHash, stateRootHash);
            cache.Add(2, blockHash, stateRootHash);
        }

        [Test]
        public void TryGetWithBlockIndex()
        {
            var cache = new BlockHashCache(1);
            Assert.IsFalse(cache.TryGet(0, out _, out _));
            var blockHash = BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash =
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(0, blockHash, stateRootHash);
            Assert.IsTrue(cache.TryGet(0, out var outBlockHash, out var outStateRootHash));
            Assert.AreEqual(blockHash, outBlockHash);
            Assert.AreEqual(stateRootHash, outStateRootHash);
            cache.Add(1, blockHash, stateRootHash);
            Assert.IsFalse(cache.TryGet(0, out _, out _));
            Assert.IsTrue(cache.TryGet(1, out outBlockHash, out outStateRootHash));
            Assert.AreEqual(blockHash, outBlockHash);
            Assert.AreEqual(stateRootHash, outStateRootHash);
        }

        [Test]
        public void TryGetWithBlockHash()
        {
            var cache = new BlockHashCache(1);
            var blockHash1 = BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash1 =
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            Assert.IsFalse(cache.TryGet(blockHash1, out _, out _));
            cache.Add(0, blockHash1, stateRootHash1);
            Assert.IsTrue(cache.TryGet(blockHash1, out var outBlockIndex1, out var outStateRootHash1));
            Assert.AreEqual(0, outBlockIndex1);
            Assert.AreEqual(stateRootHash1, outStateRootHash1);
            var blockHash2 = BlockHash.FromString("1123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash2 =
                HashDigest<SHA256>.FromString("1123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(1, blockHash2, stateRootHash2);
            Assert.IsFalse(cache.TryGet(blockHash1, out _, out _));
            Assert.IsTrue(cache.TryGet(blockHash2, out var outBlockIndex2, out var outStateRootHash2));
            Assert.AreEqual(1, outBlockIndex2);
            Assert.AreEqual(stateRootHash2, outStateRootHash2);
        }

        [Test]
        public void TryGetWithStateRootHash()
        {
            var cache = new BlockHashCache(1);
            var blockHash1 = BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash1 =
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123");
            Assert.IsFalse(cache.TryGet(stateRootHash1, out _, out _));
            cache.Add(0, blockHash1, stateRootHash1);
            Assert.IsTrue(cache.TryGet(stateRootHash1, out var outBlockIndex1, out var outBlockHash1));
            Assert.AreEqual(0, outBlockIndex1);
            Assert.AreEqual(blockHash1, outBlockHash1);
            var blockHash2 = BlockHash.FromString("1123456789012345678901234567890123456789012345678901234567890123");
            var stateRootHash2 =
                HashDigest<SHA256>.FromString("1123456789012345678901234567890123456789012345678901234567890123");
            cache.Add(1, blockHash2, stateRootHash2);
            Assert.IsFalse(cache.TryGet(stateRootHash1, out _, out _));
            Assert.IsTrue(cache.TryGet(stateRootHash2, out var outBlockIndex2, out var outBlockHash2));
            Assert.AreEqual(1, outBlockIndex2);
            Assert.AreEqual(blockHash2, outBlockHash2);
        }

        [Test]
        public void Clear()
        {
            var cache = new BlockHashCache(3);
            cache.Add(
                0,
                BlockHash.FromString("0123456789012345678901234567890123456789012345678901234567890123"),
                HashDigest<SHA256>.FromString("0123456789012345678901234567890123456789012345678901234567890123"));
            cache.Clear();
            Assert.IsFalse(cache.TryGet(0, out _, out _));
        }
    }
}
