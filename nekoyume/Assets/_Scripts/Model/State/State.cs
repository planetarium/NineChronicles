using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
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

        protected State(Dictionary serialized)
            : this(serialized["address"].ToAddress())
        {
        }

        protected State(IValue iValue) : this((Dictionary)iValue)
        {
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"address"] = address.Serialize(),
            });
    }
}
