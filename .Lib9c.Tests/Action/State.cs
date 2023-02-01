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

    public class State : IAccountStateDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _state;
        private readonly IImmutableDictionary<(Address, Currency), FungibleAssetValue> _balance;
        private readonly IImmutableDictionary<Currency, FungibleAssetValue> _totalSupplies;

        public State(
            IImmutableDictionary<Address, IValue> state = null,
            IImmutableDictionary<(Address Address, Currency Currency), FungibleAssetValue> balance = null,
            IImmutableDictionary<Currency, FungibleAssetValue> totalSupplies = null)
        {
            _state = state ?? ImmutableDictionary<Address, IValue>.Empty;
            _balance = balance ?? ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty;
            _totalSupplies =
                totalSupplies ?? ImmutableDictionary<Currency, FungibleAssetValue>.Empty;
        }

        public IImmutableSet<Address> UpdatedAddresses =>
            StateUpdatedAddresses.Union(_balance.Keys.Select(pair => pair.Item1));

        public IImmutableSet<Address> StateUpdatedAddresses =>
            _state.Keys.ToImmutableHashSet();

        public IImmutableDictionary<Address, IImmutableSet<Currency>> UpdatedFungibleAssets =>
            _balance.GroupBy(kv => kv.Key.Item1).ToImmutableDictionary(
                g => g.Key,
                g => (IImmutableSet<Currency>)g.Select(kv => kv.Key.Item2).ToImmutableHashSet()
            );

        public IImmutableSet<Currency> TotalSupplyUpdatedCurrencies =>
            _totalSupplies.Keys.ToImmutableHashSet();

        public IValue GetState(Address address) =>
            _state.TryGetValue(address, out IValue value) ? value : null;

        public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToArray();

        public IAccountStateDelta SetState(Address address, IValue state) =>
            new State(_state.SetItem(address, state), _balance);

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            _balance.TryGetValue((address, currency), out FungibleAssetValue balance) ? balance : currency * 0;

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
            if (_totalSupplies.TryGetValue(currency, out var totalSupplyValue))
            {
                return totalSupplyValue;
            }

            return currency * 0;
        }

        public IAccountStateDelta MintAsset(Address recipient, FungibleAssetValue value)
        {
            var totalSupplies =
                value.Currency.TotalSupplyTrackable
                    ? _totalSupplies.SetItem(
                        value.Currency,
                        GetTotalSupply(value.Currency) + value)
                    : _totalSupplies;
            return new State(
                _state,
                _balance.SetItem(
                    (recipient, value.Currency),
                    GetBalance(recipient, value.Currency) + value),
                totalSupplies
            );
        }

        public IAccountStateDelta BurnAsset(Address owner, FungibleAssetValue value)
        {
            var totalSupplies =
                value.Currency.TotalSupplyTrackable
                    ? _totalSupplies.SetItem(
                        value.Currency,
                        GetTotalSupply(value.Currency) - value)
                    : _totalSupplies;
            return new State(
                _state,
                _balance.SetItem(
                    (owner, value.Currency),
                    GetBalance(owner, value.Currency) - value),
                totalSupplies);
        }

        public IAccountStateDelta TransferAsset(
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

            IImmutableDictionary<(Address, Currency), FungibleAssetValue> newBalance = _balance
                .SetItem((sender, currency), senderBalance - value)
                .SetItem((recipient, currency), recipientBalance + value);
            return new State(_state, newBalance);
        }

        public IAccountStateDelta SetValidator(Validator validator)
        {
            return new State(_state);
        }

        public virtual ValidatorSet GetValidatorSet() => new ValidatorSet();
    }
}
