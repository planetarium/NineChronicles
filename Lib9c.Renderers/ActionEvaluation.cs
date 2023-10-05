using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
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

        public HashDigest<SHA256> OutputState { get; set; }

        public Exception? Exception { get; set; }

        public HashDigest<SHA256> PreviousState { get; set; }

        public int RandomSeed { get; set; }

        public Dictionary<string, IValue> Extra { get; set; }
    }
}
