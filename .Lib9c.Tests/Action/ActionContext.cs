namespace Lib9c.Tests.Action
{
    using System.Security.Cryptography;
    using Libplanet;
    using Libplanet.Action;

    public class ActionContext : IActionContext
    {
        public Address Signer { get; set; }

        public Address Miner { get; set; }

        public long BlockIndex { get; set; }

        public bool Rehearsal { get; set; }

        public IAccountStateDelta PreviousStates { get; set; }

        public IRandom Random { get; set; }

        public HashDigest<SHA256>? PreviousStateRootHash { get; set; }

        public bool BlockAction { get; }

        public IActionContext GetUnconsumedContext()
        {
            // Unable to determine if Random has ever been consumed...
            return new ActionContext
            {
                Signer = Signer,
                Miner = Miner,
                BlockIndex = BlockIndex,
                Rehearsal = Rehearsal,
                PreviousStates = PreviousStates,
                Random = Random,
                PreviousStateRootHash = PreviousStateRootHash,
            };
        }
    }
}
