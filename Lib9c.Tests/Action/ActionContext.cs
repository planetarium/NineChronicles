using Libplanet;
using Libplanet.Action;

namespace Lib9c.Tests.Action
{
    public class ActionContext : IActionContext
    {
        public Address Signer { get; set; }

        public Address Miner { get; set; }

        public long BlockIndex { get; set; }

        public bool Rehearsal { get; set; }

        public IAccountStateDelta PreviousStates { get; set; }

        public IRandom Random => throw new System.NotImplementedException();
    }
}
