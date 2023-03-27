using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Assets;
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

        public async UniTask<(Address addr, IValue value)> GetStateAsync(string searchString)
        {
            try
            {
                var addr = new Address(searchString);
                return (addr, await Agent.GetStateAsync(addr));
            }
            catch
            {
                return Aliases.ContainsKey(searchString)
                    ? (Aliases[searchString], await Agent.GetStateAsync(Aliases[searchString]))
                    : (default, default);
            }
        }

        // NOTE: Why not use <see cref="Nekoyume.BlockChain.IAgent.GetBalanceAsync()"/>?
        //       Because the implementation by
        //       <see cref="Nekoyume.BlockChain.RPCAgent.GetBalanceAsync()"/> has a bug.
        public (Address addr, FungibleAssetValue fav) GetBalance(
            Address addr,
            Currency currency)
        {
            return (addr, Agent.GetBalance(addr, currency));
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
