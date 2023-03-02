using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.BlockChain;

namespace StateViewer.Editor
{
    internal class StateProxy
    {
        public IAgent Agent { get; }
        private Dictionary<string, Address> Aliases { get; }

        public StateProxy(IAgent agent)
        {
            Agent = agent;
            Aliases = new Dictionary<string, Address>();
        }

        public IValue GetState(string searchString)
        {
            Address address;

            if (searchString.Length == 40)
            {
                address = new Address(searchString);
            }
            else
            {
                address = Aliases[searchString];
            }

            return Agent.GetState(address);
        }

        public void RegisterAlias(string alias, Address address)
        {
            if (!Aliases.ContainsKey(alias))
            {
                Aliases.Add(alias, address);
            }
            else
            {
                Aliases[alias] = address;
            }
        }
    }
}