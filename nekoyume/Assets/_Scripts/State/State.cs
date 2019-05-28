using Libplanet;

namespace Nekoyume.State
{
    public abstract class State
    {
        public Address address;

        protected State(Address address)
        {
            this.address = address;
        }
    }
}
