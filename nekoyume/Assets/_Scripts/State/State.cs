using System;
using Libplanet;

namespace Nekoyume.State
{
    [Serializable]
    public abstract class State
    {
        public Address address;

        protected State(Address address)
        {
            this.address = address;
        }
    }
}
