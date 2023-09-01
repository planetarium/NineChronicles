#nullable enable

using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Nekoyume.Blockchain;

namespace StateViewer.Runtime
{
    public class StateProxy
    {
        public IAgent Agent { get; }
        private Dictionary<string, Address> Aliases { get; }

        public StateProxy(IAgent agent)
        {
            Agent = agent;
            Aliases = new Dictionary<string, Address>();
        }

        public async UniTask<(Address? addr, IValue? value)> GetStateAsync(
            string searchString,
            long? blockIndex = null)
        {
            Address? addr = GetAddress(searchString);
            if (addr is null)
            {
                return (null, null);
            }

            return await GetStateAsync(addr.Value, blockIndex);
        }

        private Address? GetAddress(string searchString)
        {
            try
            {
                return new Address(searchString);
            }
            catch
            {
                return Aliases.TryGetValue(searchString, out var alias)
                    ? alias
                    : null;
            }
        }

        public async UniTask<(Address addr, IValue? value)> GetStateAsync(
            Address addr,
            long? blockIndex = null)
        {
            var state = await Agent.GetStateAsync(addr, blockIndex);
            return (addr, state);
        }

        public async UniTask<(Address? addr, IValue? value)> GetStateAsync(
            string searchString,
            BlockHash blockHash)
        {
            Address? addr = GetAddress(searchString);
            if (addr is null)
            {
                return (null, null);
            }

            return await GetStateAsync(addr.Value, blockHash);
        }

        public async UniTask<(Address addr, IValue? value)> GetStateAsync(
            Address addr,
            BlockHash blockHash)
        {
            var state = await Agent.GetStateAsync(addr, blockHash);
            return (addr, state);
        }

        // NOTE: Why not use <see cref="Nekoyume.Blockchain.IAgent.GetBalanceAsync()"/>?
        //       Because the implementation by
        //       <see cref="Nekoyume.Blockchain.RPCAgent.GetBalanceAsync()"/> has a bug.
        public (Address addr, FungibleAssetValue fav) GetBalance(
            Address addr,
            Currency currency)
        {
            return (addr, Agent.GetBalance(addr, currency));
        }

        public async UniTask<(Address addr, FungibleAssetValue fav)> GetBalanceAsync(
            Address addr,
            Currency currency,
            long? blockIndex)
        {
            var balance = await Agent.GetBalanceAsync(addr, currency, blockIndex);
            return (addr, balance);
        }

        public async UniTask<(Address addr, FungibleAssetValue? fav)> GetBalanceAsync(
            Address addr,
            Currency currency,
            BlockHash blockHash)
        {
            var balance = await Agent.GetBalanceAsync(addr, currency, blockHash);
            return (addr, balance);
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
