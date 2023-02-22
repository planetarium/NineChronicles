using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Tx;
using Nekoyume.Action;

namespace Lib9c.Renderers
{
    public struct ActionEvaluation<T>
        where T : ActionBase
    {
        public T Action { get; set; }

        public Address Signer { get; set; }

        public long BlockIndex { get; set; }

        public TxId? TxId { get; set; }

        public IAccountStateDelta OutputStates { get; set; }

        public Exception? Exception { get; set; }

        public IAccountStateDelta PreviousStates { get; set; }

        public int RandomSeed { get; set; }

        public Dictionary<string, IValue> Extra { get; set; }
    }
}