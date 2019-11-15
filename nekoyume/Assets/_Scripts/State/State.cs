using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.State
{
    [Serializable]
    public abstract class State : IState
    {
        public Address address;

        protected State(Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            this.address = address;
        }

        protected State(Bencodex.Types.Dictionary serialized)
            : this(serialized["address"].ToAddress())
        {
        }

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "address"] = address.Serialize(),
            });
    }
}
