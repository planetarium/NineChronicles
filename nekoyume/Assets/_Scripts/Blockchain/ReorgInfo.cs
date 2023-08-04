using System.Security.Cryptography;
using Libplanet.Common;

namespace Nekoyume.Blockchain
{
    public readonly struct ReorgInfo
    {
        public ReorgInfo(
            HashDigest<SHA256> branchpointHash,
            HashDigest<SHA256> oldTipHash,
            HashDigest<SHA256> newTipHash
        )
        {
            BranchpointHash = branchpointHash;
            OldTipHash = oldTipHash;
            NewTipHash = newTipHash;
        }

        public HashDigest<SHA256> BranchpointHash { get; }

        public HashDigest<SHA256> OldTipHash { get; }

        public HashDigest<SHA256> NewTipHash { get; }
    }
}
