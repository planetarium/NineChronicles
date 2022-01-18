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

    public class State : IAccountStateDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _state;
        private readonly IImmutableDictionary<(Address, Currency), FungibleAssetValue> _balance;

        public State(
            IImmutableDictionary<Address, IValue> state = null,
            IImmutableDictionary<(Address, Currency), FungibleAssetValue> balance = null)
        {
            _state = state ?? ImmutableDictionary<Address, IValue>.Empty;
            _balance = balance ?? ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty;
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

        public IValue GetState(Address address) =>
            _state.TryGetValue(address, out IValue value) ? value : null;

        public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToArray();

        public IAccountStateDelta SetState(Address address, IValue state) =>
            new State(_state.SetItem(address, state), _balance);

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            _balance.TryGetValue((address, currency), out FungibleAssetValue balance) ? balance : currency * 0;

        public IAccountStateDelta MintAsset(Address recipient, FungibleAssetValue value) =>
            new State(_state, _balance.SetItem((recipient, value.Currency), GetBalance(recipient, value.Currency) + value));

        public IAccountStateDelta BurnAsset(Address owner, FungibleAssetValue value) =>
            new State(_state, _balance.SetItem((owner, value.Currency), GetBalance(owner, value.Currency) - value));

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
                throw new InsufficientBalanceException(sender, senderBalance, msg);
            }

            IImmutableDictionary<(Address, Currency), FungibleAssetValue> newBalance = _balance
                .SetItem((sender, currency), senderBalance - value)
                .SetItem((recipient, currency), recipientBalance + value);
            return new State(_state, newBalance);
        }
    }
}
