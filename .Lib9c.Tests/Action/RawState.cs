namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Consensus;

    /// <summary>
    /// An implementation of <see cref="IAccountStateDelta"/> for test. It handles states as raw like Libplanet does.
    /// </summary>
    public class RawState : IAccountStateDelta
    {
        private readonly IImmutableDictionary<string, IValue> _rawStates;

        public RawState(IImmutableDictionary<string, IValue> rawStates = null)
        {
            _rawStates = rawStates ?? ImmutableDictionary<string, IValue>.Empty;
        }

        public IImmutableSet<Address> UpdatedAddresses =>
            StateUpdatedAddresses.Union(UpdatedFungibleAssets.Keys).ToImmutableHashSet();

        public IImmutableSet<Address> StateUpdatedAddresses =>
            _rawStates.Keys.Where(key => key.Length == Address.Size).Select(key => new Address(key)).ToImmutableHashSet();

        public IImmutableDictionary<Address, IImmutableSet<Currency>> UpdatedFungibleAssets =>
            throw new NotSupportedException($"Currently, {nameof(UpdatedFungibleAssets)} is not supported in this implementation.");

        public IImmutableSet<Currency> TotalSupplyUpdatedCurrencies =>
            throw new NotSupportedException($"Currently, {nameof(TotalSupplyUpdatedCurrencies)} is not supported in this implementation.");

        public IValue GetState(Address address)
        {
            return _rawStates.TryGetValue(ToStateKey(address), out IValue value) ? value : null;
        }

        public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToArray();

        public IAccountStateDelta SetState(Address address, IValue state)
        {
            return new RawState(_rawStates.SetItem(ToStateKey(address), state));
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            _rawStates.TryGetValue(ToBalanceKey(address, currency), out IValue value)
                ? FungibleAssetValue.FromRawValue(currency, value is Bencodex.Types.Integer i ? i.Value : 0)
                : currency * 0;

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
            if (_rawStates.TryGetValue(ToTotalSupplyKey(currency), out var value))
            {
                return FungibleAssetValue.FromRawValue(currency, value is Bencodex.Types.Integer i ? i.Value : 0);
            }

            return currency * 0;
        }

        public IAccountStateDelta MintAsset(Address recipient, FungibleAssetValue value)
        {
            var currency = value.Currency;
            var rawStates = _rawStates.SetItem(
                    ToBalanceKey(recipient, currency),
                    (Bencodex.Types.Integer)(GetBalance(recipient, currency) + value).RawValue);
            if (value.Currency.TotalSupplyTrackable)
            {
                rawStates = rawStates.SetItem(
                    ToTotalSupplyKey(currency),
                    (Bencodex.Types.Integer)(GetTotalSupply(currency) + value).RawValue);
            }

            return new RawState(rawStates);
        }

        public IAccountStateDelta TransferAsset(
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false)
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

            IImmutableDictionary<string, IValue> newRawStates = _rawStates
                .SetItem(ToBalanceKey(sender, currency), (Bencodex.Types.Integer)(senderBalance - value).RawValue)
                .SetItem(ToBalanceKey(recipient, currency), (Bencodex.Types.Integer)(recipientBalance + value).RawValue);
            return new RawState(newRawStates);
        }

        public IAccountStateDelta BurnAsset(Address owner, FungibleAssetValue value)
        {
            var currency = value.Currency;
            var rawStates = _rawStates.SetItem(
                ToBalanceKey(owner, currency),
                (Bencodex.Types.Integer)(GetBalance(owner, currency) - value).RawValue);
            if (value.Currency.TotalSupplyTrackable)
            {
                rawStates = rawStates.SetItem(
                    ToTotalSupplyKey(currency),
                    (Bencodex.Types.Integer)(GetTotalSupply(currency) - value).RawValue);
            }

            return new RawState(rawStates);
        }

        public IAccountStateDelta SetValidator(Validator validator)
        {
            return new RawState(_rawStates);
        }

        public virtual ValidatorSet GetValidatorSet() => new ValidatorSet();

        private string ToStateKey(Address address) => address.ToHex().ToLowerInvariant();

        private string ToBalanceKey(Address address, Currency currency) => "_" + address.ToHex().ToLowerInvariant() +
                                                                           "_" + ByteUtil.Hex(currency.Hash.ByteArray);

        private string ToTotalSupplyKey(Currency currency) => "__" + ByteUtil.Hex(currency.Hash.ByteArray);
    }
}
