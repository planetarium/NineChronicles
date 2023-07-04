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

    public class State : IAccountStateDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _state;
        private readonly IImmutableDictionary<(Address, Currency), BigInteger> _balance;
        private readonly IImmutableDictionary<Currency, BigInteger> _totalSupplies;
        private readonly ValidatorSet _validatorSet;
        private readonly IAccountDelta _delta;

        public State()
            : this(
                ImmutableDictionary<Address, IValue>.Empty,
                ImmutableDictionary<(Address Address, Currency Currency), BigInteger>.Empty,
                ImmutableDictionary<Currency, BigInteger>.Empty,
                new ValidatorSet())
        {
        }

        // Pretends all given arguments are part of the delta, i.e., have been modified
        // using appropriate methods such as Transfer/Mint/Burn to set the values.
        // Also convert to internal data types.
        public State(
            IImmutableDictionary<Address, IValue> state = null,
            IImmutableDictionary<(Address Address, Currency Currency), FungibleAssetValue> balance = null,
            IImmutableDictionary<Currency, FungibleAssetValue> totalSupplies = null,
            ValidatorSet validatorSet = null)
        {
            _state = state ?? ImmutableDictionary<Address, IValue>.Empty;
            _balance = balance is { } b
                ? b.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.RawValue)
                : ImmutableDictionary<(Address, Currency), BigInteger>.Empty;
            _totalSupplies = totalSupplies is { } t
                ? t.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.RawValue)
                : ImmutableDictionary<Currency, BigInteger>.Empty;
            _validatorSet =
                validatorSet ?? new ValidatorSet();

            _delta = new Delta(_state, _balance, _totalSupplies, _validatorSet);
        }

        // For Transfer/Mint/Burn
        private State(
            IImmutableDictionary<Address, IValue> state,
            IImmutableDictionary<(Address Address, Currency Currency), BigInteger> balance,
            IImmutableDictionary<Currency, BigInteger> totalSupplies,
            ValidatorSet validatorSet)
        {
            _state = state;
            _balance = balance;
            _totalSupplies = totalSupplies;
            _validatorSet = validatorSet;

            _delta = new Delta(_state, _balance, _totalSupplies, _validatorSet);
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
            new State(
                _state.SetItem(address, state),
                _balance,
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
            return new State(
                _state,
                _balance.SetItem(
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
            return new State(
                _state,
                _balance.SetItem(
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

            IImmutableDictionary<(Address, Currency), BigInteger> newBalance = _balance
                .SetItem((sender, currency), (senderBalance - value).RawValue)
                .SetItem((recipient, currency), (recipientBalance + value).RawValue);
            return new State(_state, newBalance, _totalSupplies, _validatorSet);
        }

        public IAccountStateDelta SetValidator(Validator validator)
        {
            return new State(_state, _balance, _totalSupplies, GetValidatorSet().Update(validator));
        }

        public ValidatorSet GetValidatorSet() => _validatorSet;
    }

#pragma warning disable SA1402
    public class Delta : IAccountDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _state;
        private readonly IImmutableDictionary<(Address, Currency), BigInteger> _balance;
        private readonly IImmutableDictionary<Currency, BigInteger> _totalSupplies;
        private readonly ValidatorSet _validatorSet;

        public Delta()
            : this(
                ImmutableDictionary<Address, IValue>.Empty,
                ImmutableDictionary<(Address, Currency), BigInteger>.Empty,
                ImmutableDictionary<Currency, BigInteger>.Empty,
                null)
        {
        }

        public Delta(
            IImmutableDictionary<Address, IValue> state,
            IImmutableDictionary<(Address Address, Currency Currency), BigInteger> balance,
            IImmutableDictionary<Currency, BigInteger> totalSupplies,
            ValidatorSet validatorSet)
        {
            _state = state;
            _balance = balance;
            _totalSupplies = totalSupplies;
            _validatorSet = validatorSet;
        }

        public IImmutableSet<Address> UpdatedAddresses =>
            StateUpdatedAddresses.Union(FungibleUpdatedAddresses);

        public IImmutableSet<Address> StateUpdatedAddresses => _state.Keys.ToImmutableHashSet();

        public IImmutableDictionary<Address, IValue> States => _state;

        public IImmutableSet<Address> FungibleUpdatedAddresses =>
            UpdatedFungibleAssets.Select(pair => pair.Item1).ToImmutableHashSet();

        public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets =>
            Fungibles.Keys.ToImmutableHashSet();

        public IImmutableDictionary<(Address, Currency), BigInteger> Fungibles =>
            _balance;

        public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies =>
            TotalSupplies.Keys.ToImmutableHashSet();

        public IImmutableDictionary<Currency, BigInteger> TotalSupplies =>
            _totalSupplies;

        public ValidatorSet ValidatorSet => _validatorSet;
    }
#pragma warning restore SA1402
}
