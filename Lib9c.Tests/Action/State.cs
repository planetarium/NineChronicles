namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;

    public class State : IAccountStateDelta
    {
        private readonly IImmutableDictionary<Address, IValue> _state;
        private readonly IImmutableDictionary<(Address, Currency), BigInteger> _balance;

        public State(
            IImmutableDictionary<Address, IValue> state = null,
            IImmutableDictionary<(Address, Currency), BigInteger> balance = null)
        {
            _state = state ?? ImmutableDictionary<Address, IValue>.Empty;
            _balance = balance ?? ImmutableDictionary<(Address, Currency), BigInteger>.Empty;
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

        public IAccountStateDelta SetState(Address address, IValue state) =>
            new State(_state.SetItem(address, state), _balance);

        public BigInteger GetBalance(Address address, Currency currency) =>
            _balance.TryGetValue((address, currency), out BigInteger balance) ? balance : 0;

        public IAccountStateDelta MintAsset(Address recipient, Currency currency, BigInteger amount) =>
            new State(_state, _balance.SetItem((recipient, currency), GetBalance(recipient, currency) + amount));

        public IAccountStateDelta BurnAsset(Address owner, Currency currency, BigInteger amount) =>
            new State(_state, _balance.SetItem((owner, currency), GetBalance(owner, currency) - amount));

        public IAccountStateDelta TransferAsset(
            Address sender,
            Address recipient,
            Currency currency,
            BigInteger amount,
            bool allowNegativeBalance = false
        )
        {
            // Libplanet 코드에서 들고 옴... (AccountStateDeltaImpl.cs@d365e474d5abf9377f553376a38b93982a36d5f0)
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "The amount to transfer has to be greater than 0."
                );
            }

            BigInteger senderBalance = GetBalance(sender, currency),
                       recipientBalance = GetBalance(recipient, currency);

            if (!allowNegativeBalance && senderBalance < amount)
            {
                var msg = $"The account {sender}'s balance of {currency} is insufficient to " +
                          $"transfer: {senderBalance} {currency} < {amount} {currency}.";
                throw new InsufficientBalanceException(sender, currency, senderBalance, msg);
            }

            IImmutableDictionary<(Address, Currency), BigInteger> newBalance = _balance
                .SetItem((sender, currency), senderBalance - amount)
                .SetItem((recipient, currency), recipientBalance + amount);
            return new State(_state, newBalance);
        }
    }
}
