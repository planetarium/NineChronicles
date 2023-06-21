namespace Lib9c.Tests.Action
{
    using System.Security.Cryptography;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blocks;
    using Libplanet.State;
    using Libplanet.Tx;

    public class ActionContext : IActionContext
    {
        private long _gasUsed;

        public BlockHash? GenesisHash { get; set; }

        public Address Signer { get; set; }

        public TxId? TxId { get; set; }

        public Address Miner { get; set; }

        public BlockHash BlockHash { get; set; }

        public long BlockIndex { get; set; }

        public bool Rehearsal { get; set; }

        public IAccountStateDelta PreviousStates { get; set; }

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
                Rehearsal = Rehearsal,
                PreviousStates = PreviousStates,
                Random = Random,
                PreviousStateRootHash = PreviousStateRootHash,
            };
        }

        public long GasUsed() => _gasUsed;

        public long GasLimit() => 0;

        public void PutLog(string log)
        {
        }
    }
}
