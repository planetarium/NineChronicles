namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Consensus;
    using Libplanet.State;

    public class MockStateDelta : IAccountStateDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _states;
        private readonly IImmutableDictionary<(Address, Currency), BigInteger> _fungibles;
        private readonly IImmutableDictionary<Currency, BigInteger> _totalSupplies;
        private readonly ValidatorSet _validatorSet;
        private readonly IAccountDelta _delta;

        public MockStateDelta()
            : this(MockState.Empty)
        {
        }

        public MockStateDelta(MockState mockState)
            : this(
                mockState.States,
                mockState.Fungibles,
                mockState.TotalSupplies,
                mockState.ValidatorSet)
        {
        }

        // Pretends all given arguments are part of the delta, i.e., have been modified
        // using appropriate methods such as Transfer/Mint/Burn to set the values.
        // Also convert to internal data types.
        public MockStateDelta(
            IImmutableDictionary<Address, IValue> states = null,
            IImmutableDictionary<(Address Address, Currency Currency), FungibleAssetValue> balances = null,
            IImmutableDictionary<Currency, FungibleAssetValue> totalSupplies = null,
            ValidatorSet validatorSet = null)
        {
            _states = states ?? ImmutableDictionary<Address, IValue>.Empty;
            _fungibles = balances is { } b
                ? b.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.RawValue)
                : ImmutableDictionary<(Address, Currency), BigInteger>.Empty;
            _totalSupplies = totalSupplies is { } t
                ? t.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.RawValue)
                : ImmutableDictionary<Currency, BigInteger>.Empty;
            _validatorSet =
                validatorSet ?? new ValidatorSet();

            _delta = new MockDelta(_states, _fungibles, _totalSupplies, _validatorSet);
        }

        // For Transfer/Mint/Burn
        private MockStateDelta(
            IImmutableDictionary<Address, IValue> state,
            IImmutableDictionary<(Address Address, Currency Currency), BigInteger> balance,
            IImmutableDictionary<Currency, BigInteger> totalSupplies,
            ValidatorSet validatorSet)
        {
            _states = state;
            _fungibles = balance;
            _totalSupplies = totalSupplies;
            _validatorSet = validatorSet;

            _delta = new MockDelta(_states, _fungibles, _totalSupplies, _validatorSet);
        }

        public IAccountDelta Delta => _delta;

        public IImmutableSet<Address> UpdatedAddresses => _delta.UpdatedAddresses;

        public IImmutableSet<Address> StateUpdatedAddresses => _delta.StateUpdatedAddresses;

        public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets => _delta.UpdatedFungibleAssets;

        public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets => _delta.UpdatedFungibleAssets;

        public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies => _delta.UpdatedTotalSupplyCurrencies;

        public IValue GetState(Address address) => _delta.States.TryGetValue(address, out IValue value)
            ? value
            : null;

        public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToArray();

        public IAccountStateDelta SetState(Address address, IValue state) =>
            new MockStateDelta(
                _states.SetItem(address, state),
                _fungibles,
                _totalSupplies,
                _validatorSet);

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            _delta.Fungibles.TryGetValue((address, currency), out BigInteger rawValue)
                ? FungibleAssetValue.FromRawValue(currency, rawValue)
                : FungibleAssetValue.FromRawValue(currency, 0);

        public FungibleAssetValue GetTotalSupply(Currency currency)
        {
            if (!currency.TotalSupplyTrackable)
            {
                var msg =
                    $"The total supply value of the currency {currency} is not trackable"
                    + " because it is a legacy untracked currency which might have been"
                    + " established before the introduction of total supply tracking support.";
                throw new TotalSupplyNotTrackableException(msg, currency);
            }

            // Return dirty state if it exists.
            return _delta.TotalSupplies.TryGetValue(currency, out var rawValue)
                ? FungibleAssetValue.FromRawValue(currency, rawValue)
                : FungibleAssetValue.FromRawValue(currency, 0);
        }

        public IAccountStateDelta MintAsset(IActionContext context, Address recipient, FungibleAssetValue value)
        {
            var totalSupplies =
                value.Currency.TotalSupplyTrackable
                    ? _totalSupplies.SetItem(
                        value.Currency,
                        (GetTotalSupply(value.Currency) + value).RawValue)
                    : _totalSupplies;
            return new MockStateDelta(
                _states,
                _fungibles.SetItem(
                    (recipient, value.Currency),
                    (GetBalance(recipient, value.Currency) + value).RawValue),
                totalSupplies,
                _validatorSet
            );
        }

        public IAccountStateDelta BurnAsset(IActionContext context, Address owner, FungibleAssetValue value)
        {
            var totalSupplies =
                value.Currency.TotalSupplyTrackable
                    ? _totalSupplies.SetItem(
                        value.Currency,
                        (GetTotalSupply(value.Currency) - value).RawValue)
                    : _totalSupplies;
            return new MockStateDelta(
                _states,
                _fungibles.SetItem(
                    (owner, value.Currency),
                    (GetBalance(owner, value.Currency) - value).RawValue),
                totalSupplies,
                _validatorSet);
        }

        public IAccountStateDelta TransferAsset(
            IActionContext context,
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false
        )
        {
            // Copy from Libplanet (AccountStateDeltaImpl.cs@66104588af35afbd18a41bb7857eacd9da190019)
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The value to transfer has to be greater than zero."
                );
            }

            Currency currency = value.Currency;
            FungibleAssetValue senderBalance = GetBalance(sender, currency);
            FungibleAssetValue recipientBalance = GetBalance(recipient, currency);

            if (!allowNegativeBalance && senderBalance < value)
            {
                var msg = $"The account {sender}'s balance of {currency} is insufficient to " +
                          $"transfer: {senderBalance} < {value}.";
                throw new InsufficientBalanceException(msg, sender, senderBalance);
            }

            IImmutableDictionary<(Address, Currency), BigInteger> newBalance = _fungibles
                .SetItem((sender, currency), (senderBalance - value).RawValue)
                .SetItem((recipient, currency), (recipientBalance + value).RawValue);
            return new MockStateDelta(_states, newBalance, _totalSupplies, _validatorSet);
        }

        public IAccountStateDelta SetValidator(Validator validator)
        {
            return new MockStateDelta(_states, _fungibles, _totalSupplies, GetValidatorSet().Update(validator));
        }

        public ValidatorSet GetValidatorSet() => _validatorSet;
    }
}
