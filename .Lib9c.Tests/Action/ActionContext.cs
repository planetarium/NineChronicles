namespace Lib9c.Tests.Action
{
    using System.Security.Cryptography;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;

    public class ActionContext : IActionContext
    {
        private long _gasUsed;
        private IRandom _random = null;

        public BlockHash? GenesisHash { get; set; }

        public Address Signer { get; set; }

        public TxId? TxId { get; set; }

        public Address Miner { get; set; }

        public BlockHash BlockHash { get; set; }

        public long BlockIndex { get; set; }

        public int BlockProtocolVersion { get; set; }

        public bool Rehearsal { get; set; }

        public IAccount PreviousState { get; set; }

        public int RandomSeed { get; set; }

        public HashDigest<SHA256>? PreviousStateRootHash { get; set; }

        public bool BlockAction { get; }

        public void UseGas(long gas)
        {
            _gasUsed += gas;
        }

        public IRandom GetRandom() => _random ?? new TestRandom(RandomSeed);

        public long GasUsed() => _gasUsed;

        public long GasLimit() => 0;

        // FIXME: Temporary measure to allow inheriting already mutated IRandom.
        public void SetRandom(IRandom random)
        {
            _random = random;
        }
    }
}
