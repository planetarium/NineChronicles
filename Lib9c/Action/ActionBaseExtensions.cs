using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;

namespace Nekoyume.Action
{
    public static class ActionBaseExtensions
    {
        public static IImmutableSet<Address> CalculateUpdateAddresses(this IEnumerable<ActionBase> actions)
        {
            IImmutableSet<Address> addresses = ImmutableHashSet<Address>.Empty;
            IActionContext rehearsalContext = new RehearsalActionContext();

            foreach (ActionBase action in actions)
            {
                try
                {
                    IAccountStateDelta nextStates = action.Execute(rehearsalContext);
                    addresses = addresses.Union(nextStates.Delta.UpdatedAddresses);
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

            public int BlockProtocolVersion => default;

            public bool Rehearsal => true;

            public IAccountStateDelta PreviousState => new AddressTraceStateDelta();

            public IRandom Random => default;

            public HashDigest<SHA256>? PreviousStateRootHash => default;

            public bool BlockAction => default;

            public void UseGas(long gas)
            {
                // pass
            }

            public IActionContext GetUnconsumedContext() => null;

            public long GasUsed() => 0;

            public long GasLimit() => 0;
        }

        private class AddressTraceStateDelta : IAccountStateDelta
        {
            private AddressTraceDelta _delta;

            public AddressTraceStateDelta()
                : this(new AddressTraceDelta())
            {
            }

            public AddressTraceStateDelta(AddressTraceDelta delta)
            {
                _delta = delta;
            }

            public IAccountDelta Delta => _delta;

            public IImmutableSet<Address> UpdatedAddresses => _delta.UpdatedAddresses;

            public IImmutableSet<Address> StateUpdatedAddresses => _delta.StateUpdatedAddresses;

            public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets =>
                Delta.UpdatedFungibleAssets;

            public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets =>
                throw new NotSupportedException();

            public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies
                => Delta.UpdatedTotalSupplyCurrencies;

            public IAccountStateDelta BurnAsset(IActionContext context, Address owner, FungibleAssetValue value)
            {
                return new AddressTraceStateDelta(
                    new AddressTraceDelta(Delta.UpdatedAddresses.Union(new [] { owner })));
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

            public IAccountStateDelta MintAsset(IActionContext context, Address recipient, FungibleAssetValue value)
            {
                return new AddressTraceStateDelta(
                    new AddressTraceDelta(Delta.UpdatedAddresses.Union(new[] { recipient })));
            }

            public IAccountStateDelta SetState(Address address, IValue state)
            {
                return new AddressTraceStateDelta(
                    new AddressTraceDelta(Delta.UpdatedAddresses.Union(new[] { address })));
            }

            public IAccountStateDelta TransferAsset(
                IActionContext context,
                Address sender,
                Address recipient,
                FungibleAssetValue value,
                bool allowNegativeBalance = false
            )
            {
                return new AddressTraceStateDelta(
                    new AddressTraceDelta(Delta.UpdatedAddresses.Union(new[] { sender, recipient })));
            }

            public ValidatorSet GetValidatorSet() => throw new NotSupportedException();

            public IAccountStateDelta SetValidator(Validator validator)
            {
                throw new NotSupportedException();
            }

            public class AddressTraceDelta : IAccountDelta
            {
                private IImmutableSet<Address> _updatedAddresses;

                public AddressTraceDelta()
                    : this(ImmutableHashSet<Address>.Empty)
                {
                }

                public AddressTraceDelta(IImmutableSet<Address> updatedAddresses)
                {
                    _updatedAddresses = updatedAddresses;
                }

                public IImmutableSet<Address> UpdatedAddresses => _updatedAddresses;
                public IImmutableSet<Address> StateUpdatedAddresses => _updatedAddresses;
                public IImmutableDictionary<Address, IValue> States => throw new NotSupportedException();
                public IImmutableSet<Address> FungibleUpdatedAddresses => _updatedAddresses;
                public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets => throw new NotSupportedException();
                public IImmutableDictionary<(Address, Currency), BigInteger> Fungibles => throw new NotSupportedException();
                public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies => throw new NotSupportedException();
                public IImmutableDictionary<Currency, BigInteger> TotalSupplies => throw new NotSupportedException();
                public ValidatorSet ValidatorSet => throw new NotSupportedException();
            }
        }
    }
}
