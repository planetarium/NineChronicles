using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    public abstract class State : IState
    {
        public Address address;

        protected State(Address address)
        {
            this.address = address;
        }

        protected State(Dictionary serialized)
            : this(serialized.ContainsKey(LegacyAddressKey)
                ? serialized[LegacyAddressKey].ToAddress()
                : serialized[AddressKey].ToAddress())
        {
        }

        protected State(IValue iValue) : this((Dictionary)iValue)
        {
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = address.Serialize(),
            });
        public virtual IValue SerializeV2() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)AddressKey] = address.Serialize(),
            });

    }
}
