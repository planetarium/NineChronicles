namespace Lib9c.Tests.Action
{
    using System.Security.Cryptography;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;

    public class ActionContext : IActionContext
    {
        private long _gasUsed;

        public BlockHash? GenesisHash { get; set; }

        public Address Signer { get; set; }

        public TxId? TxId { get; set; }

        public Address Miner { get; set; }

        public BlockHash BlockHash { get; set; }

        public long BlockIndex { get; set; }

        public int BlockProtocolVersion { get; set; }

        public bool Rehearsal { get; set; }

        public IAccountStateDelta PreviousState { get; set; }

        public IRandom Random { get; set; }

        public HashDigest<SHA256>? PreviousStateRootHash { get; set; }

        public bool BlockAction { get; }

        public bool IsNativeToken(Currency currency) => false;

        public void UseGas(long gas)
        {
            _gasUsed += gas;
        }

        public IActionContext GetUnconsumedContext()
        {
            // Unable to determine if Random has ever been consumed...
            return new ActionContext
            {
                Signer = Signer,
                TxId = TxId,
                Miner = Miner,
                BlockHash = BlockHash,
                BlockIndex = BlockIndex,
                BlockProtocolVersion = BlockProtocolVersion,
                Rehearsal = Rehearsal,
                PreviousState = PreviousState,
                Random = Random,
                PreviousStateRootHash = PreviousStateRootHash,
            };
        }

        public long GasUsed() => _gasUsed;

        public long GasLimit() => 0;
    }
}
