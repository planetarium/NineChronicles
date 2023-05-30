using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blocks;
using Libplanet.Tx;

namespace Nekoyume.Action
{
    public static class ActionBaseExtensions
    {
        public static IImmutableSet<Address> CalculateUpdateAddresses(
            this IEnumerable<PolymorphicAction<ActionBase>> actions
        ) => CalculateUpdateAddresses(actions.Select(pa => pa.InnerAction));

        public static IImmutableSet<Address> CalculateUpdateAddresses(this IEnumerable<ActionBase> actions)
        {
            IImmutableSet<Address> addresses = ImmutableHashSet<Address>.Empty;
            IActionContext rehearsalContext = new RehearsalActionContext();

            foreach (ActionBase action in actions)
            {
                try
                {
                    IAccountStateDelta nextStates = action.Execute(rehearsalContext);
                    addresses = addresses.Union(nextStates.UpdatedAddresses);
                }
                catch (NotSupportedException)
                {
                    // Ignore updated addresses from incompatible actions
                }
            }

            return addresses;
        }

        private class RehearsalActionContext : IActionContext
        {
            public BlockHash? GenesisHash => default;

            public Address Signer => default;

            public TxId? TxId => default;

            public Address Miner => default;

            public long BlockIndex => default;

            public bool Rehearsal => true;

            public IAccountStateDelta PreviousStates => new AddressTraceStateDelta();

            public IRandom Random => default;

            public HashDigest<SHA256>? PreviousStateRootHash => default;

            public bool BlockAction => default;

            public IActionContext GetUnconsumedContext() => null;

            public long GasUsed() => 0;

            public long GasLimit() => 0;

            public void PutLog(string log)
            {
                // Method intentionally left empty.
            }
        }

        private class AddressTraceStateDelta : IAccountStateDelta
        {
            private ImmutableHashSet<Address> _updatedAddresses;

            public AddressTraceStateDelta()
                : this(ImmutableHashSet<Address>.Empty)
            {
            }

            public AddressTraceStateDelta(ImmutableHashSet<Address> updatedAddresses)
            {
                _updatedAddresses = updatedAddresses;
            }

            public IImmutableSet<Address> UpdatedAddresses => _updatedAddresses;

            public IImmutableSet<Address> StateUpdatedAddresses => _updatedAddresses;

            public IImmutableDictionary<Address, IImmutableSet<Currency>> UpdatedFungibleAssets
                => ImmutableDictionary<Address, IImmutableSet<Currency>>.Empty;

            public IImmutableSet<Currency> TotalSupplyUpdatedCurrencies
                => ImmutableHashSet<Currency>.Empty;

            public IAccountStateDelta BurnAsset(Address owner, FungibleAssetValue value)
            {
                return new AddressTraceStateDelta(_updatedAddresses.Union(new [] { owner }));
            }

            public FungibleAssetValue GetBalance(Address address, Currency currency)
            {
                throw new NotSupportedException();
            }

            public IValue GetState(Address address)
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses)
            {
                throw new NotSupportedException();
            }

            public FungibleAssetValue GetTotalSupply(Currency currency)
            {
                throw new NotSupportedException();
            }

            public IAccountStateDelta MintAsset(Address recipient, FungibleAssetValue value)
            {
                return new AddressTraceStateDelta(_updatedAddresses.Union(new[] { recipient }));
            }

            public IAccountStateDelta SetState(Address address, IValue state)
            {
                return new AddressTraceStateDelta(_updatedAddresses.Union(new[] { address }));
            }

            public IAccountStateDelta TransferAsset(
                Address sender,
                Address recipient,
                FungibleAssetValue value,
                bool allowNegativeBalance = false
            )
            {
                return new AddressTraceStateDelta(
                    _updatedAddresses.Union(new[] { sender, recipient })
                );
            }
        }
    }
}
