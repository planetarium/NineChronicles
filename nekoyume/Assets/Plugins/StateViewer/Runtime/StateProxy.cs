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

        public async UniTask<(Address? accountAddr, Address? addr, IValue? value)> GetStateAsync(
            string accountAddressString,
            string addressString)
        {
            Address? accountAddr = GetAddress(accountAddressString);
            Address? addr = GetAddress(addressString);
            if (accountAddr is null || addr is null)
            {
                return (null, null, null);
            }

            return await GetStateAsync(accountAddr.Value, addr.Value);
        }

        public async UniTask<(Address? accountAddr, Address? addr, IValue? value)> GetStateAsync(
            long blockIndex,
            string accountAddressString,
            string addressString)
        {
            Address? accountAddr = GetAddress(accountAddressString);
            Address? addr = GetAddress(addressString);
            if (accountAddr is null || addr is null)
            {
                return (null, null, null);
            }

            return await GetStateAsync(blockIndex, accountAddr.Value, addr.Value);
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

        public async UniTask<(Address accountAddr, Address addr, IValue? value)> GetStateAsync(
            Address accountAddr,
            Address addr)
        {
            var state = await Agent.GetStateAsync(accountAddr, addr);
            return (accountAddr, addr, state);
        }

        public async UniTask<(Address accountAddr, Address addr, IValue? value)> GetStateAsync(
            long blockIndex,
            Address accountAddr,
            Address addr)
        {
            var state = await Agent.GetStateAsync(blockIndex, accountAddr, addr);
            return (accountAddr, addr, state);
        }

        public async UniTask<(Address? accountAddr, Address? addr, IValue? value)> GetStateAsync(
            BlockHash blockHash,
            string accountAddressString,
            string addressString)
        {
            Address? accountAddr = GetAddress(accountAddressString);
            Address? addr = GetAddress(addressString);
            if (accountAddr is null || addr is null)
            {
                return (null, null, null);
            }

            return await GetStateAsync(blockHash, accountAddr.Value, addr.Value);
        }

        public async UniTask<(Address accountAddr, Address addr, IValue? value)> GetStateAsync(
            BlockHash blockHash,
            Address accountAddr,
            Address addr)
        {
            var state = await Agent.GetStateAsync(blockHash, accountAddr, addr);
            return (accountAddr, addr, state);
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
            Currency currency)
        {
            var balance = await Agent.GetBalanceAsync(addr, currency);
            return (addr, balance);
        }

        public async UniTask<(Address addr, FungibleAssetValue fav)> GetBalanceAsync(
            long blockIndex,
            Address addr,
            Currency currency)
        {
            var balance = await Agent.GetBalanceAsync(blockIndex, addr, currency);
            return (addr, balance);
        }

        public async UniTask<(Address addr, FungibleAssetValue? fav)> GetBalanceAsync(
            BlockHash blockHash,
            Address addr,
            Currency currency)
        {
            var balance = await Agent.GetBalanceAsync(blockHash, addr, currency);
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
