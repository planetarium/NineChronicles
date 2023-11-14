using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    public readonly struct FungibleItemValue
    {
        public FungibleItemValue(List bencoded)
            : this(
                new HashDigest<SHA256>((Binary)bencoded[0]),
                (Integer)bencoded[1]
            )
        {
        }

        public FungibleItemValue(HashDigest<SHA256> id, int count)
        {
            Id = id;
            Count = count;
        }

        public IValue Serialize()
        {
            return new List(Id.Serialize(), (Integer)Count);
        }

        public HashDigest<SHA256> Id { get; }
        public int Count { get; }
    }
}
