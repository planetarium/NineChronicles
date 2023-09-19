using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class AccountStateDelta : IAccount
{
    private IImmutableDictionary<Address, IValue> _states;
    private IImmutableDictionary<(Address, Currency), BigInteger> _fungibles;
    private IImmutableDictionary<Currency, BigInteger> _totalSupplies;
    private ValidatorSet? _validatorSet;
    private IAccountDelta _delta;

    public IAccountState BaseState { get; set; }

    public IImmutableSet<Address> UpdatedAddresses => _delta.UpdatedAddresses;

    public IImmutableSet<Address> StateUpdatedAddresses => _delta.StateUpdatedAddresses;

    public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets => _delta.UpdatedFungibleAssets;

#pragma warning disable LAA1002
    public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets { get; }
#pragma warning restore LAA1002

    public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies => _delta.UpdatedTotalSupplyCurrencies;

    public AccountStateDelta()
        : this(
            ImmutableDictionary<Address, IValue>.Empty,
            ImmutableDictionary<(Address, Currency), BigInteger>.Empty,
            ImmutableDictionary<Currency, BigInteger>.Empty,
            null)
    {
    }

    public AccountStateDelta(
        IImmutableDictionary<Address, IValue> states,
        IImmutableDictionary<(Address, Currency), BigInteger> fungibles,
        IImmutableDictionary<Currency, BigInteger> totalSupplies,
        ValidatorSet? validatorSet
    )
    {
        _delta = new AccountDelta(
            states,
            fungibles,
            totalSupplies,
            validatorSet);
        _states = states;
        _fungibles = fungibles;
        _totalSupplies = totalSupplies;
        _validatorSet = validatorSet;
    }

    public AccountStateDelta(Dictionary states, List fungibles, List totalSupplies, IValue validatorSet)
    {
        // This assumes `states` consists of only Binary keys:
        _states = states
            .ToImmutableDictionary(
                kv => new Address(((Binary)kv.Key).ByteArray),
                kv => kv.Value);

        _fungibles = fungibles
            .Cast<Dictionary>()
            .Select(dict =>
                new KeyValuePair<(Address, Currency), BigInteger>(
                    (
                        new Address(((Binary)dict["address"]).ByteArray),
                        new Currency(dict["currency"])
                    ),
                    ((Integer)dict["amount"]).Value
                ))
            .ToImmutableDictionary();

        // This assumes `totalSupplies` consists of only Binary keys:
        _totalSupplies = totalSupplies
            .Cast<Dictionary>()
            .Select(dict =>
                new KeyValuePair<Currency, BigInteger>(
                    new Currency(dict["currency"]),
                    new BigInteger((Integer)dict["amount"])))
            .ToImmutableDictionary();

        _validatorSet = validatorSet is Null
            ? null
            : new ValidatorSet(validatorSet);

        _delta = new AccountDelta(
            _states,
            _fungibles,
            _totalSupplies,
            _validatorSet);
    }

    public AccountStateDelta(IValue serialized)
        : this((Dictionary)serialized)
    {
    }

    public AccountStateDelta(Dictionary dict)
        : this(
            (Dictionary)dict["states"],
            (List)dict["balances"],
            (List)dict["totalSupplies"],
            dict["validatorSet"])
    {
    }

    public AccountStateDelta(byte[] bytes)
        : this((Dictionary)new Codec().Decode(bytes))
    {
    }

    public ITrie Trie => throw new NotSupportedException();

    public IAccountDelta Delta => _delta;

    public ITrie Trie
    {
        get;
    }
    public IValue? GetState(Address address) =>
        _states.ContainsKey(address)
            ? _states[address]
            : BaseState.GetState(address);

    public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses) =>
        addresses.Select(GetState).ToArray();

    public IAccount SetState(Address address, IValue state) =>
        new AccountStateDelta(_states.SetItem(address, state), _fungibles, _totalSupplies, _validatorSet);
    public FungibleAssetValue GetBalance(Address address, Currency currency)
    {
        if (!_fungibles.TryGetValue((address, currency), out BigInteger rawValue))
        {
            return BaseState.GetBalance(address, currency);
        }

        return FungibleAssetValue.FromRawValue(currency, rawValue);
    }

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
            return FungibleAssetValue.FromRawValue(currency, totalSupplyValue);
        }

        return BaseState.GetTotalSupply(currency);
    }

    public IAccount MintAsset(
        IActionContext context, Address recipient, FungibleAssetValue value)
    {
        // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
        // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

        var currency = value.Currency;

        if (value <= currency * 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var nextAmount = GetBalance(recipient, value.Currency) + value;

        if (currency.TotalSupplyTrackable)
        {
            var currentTotalSupply = GetTotalSupply(currency);
            if (currency.MaximumSupply < currentTotalSupply + value)
            {
                var msg = $"The amount {value} attempted to be minted added to the current"
                          + $" total supply of {currentTotalSupply} exceeds the"
                          + $" maximum allowed supply of {currency.MaximumSupply}.";
                throw new SupplyOverflowException(msg, value);
            }

            return new AccountStateDelta(
                _states,
                _fungibles.SetItem(
                    (recipient, value.Currency),
                    nextAmount.RawValue
                ),
                _totalSupplies.SetItem(currency, (currentTotalSupply + value).RawValue),
                _validatorSet
            )
            {
                BaseState = BaseState,
            };
        }

        return new AccountStateDelta(
            _states,
            _fungibles.SetItem(
                (recipient, value.Currency),
                nextAmount.RawValue
            ),
            _totalSupplies,
            _validatorSet
        )
        {
            BaseState = BaseState,
        };
    }

    public IAccount TransferAsset(
        IActionContext context,
        Address sender,
        Address recipient,
        FungibleAssetValue value,
        bool allowNegativeBalance = false)
    {
        if (value.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        FungibleAssetValue senderBalance = GetBalance(sender, value.Currency);
        if (senderBalance < value)
        {
            throw new InsufficientBalanceException(
                $"There is no sufficient balance for {sender}: {senderBalance} < {value}",
                sender,
                senderBalance
            );
        }

        Currency currency = value.Currency;
        FungibleAssetValue senderRemains = senderBalance - value;
        FungibleAssetValue recipientRemains = GetBalance(recipient, currency) + value;
        var balances = _fungibles
            .SetItem((sender, currency), senderRemains.RawValue)
            .SetItem((recipient, currency), recipientRemains.RawValue);
        return new AccountStateDelta(_states, balances, _totalSupplies, _validatorSet)
        {
            BaseState = BaseState,
        };
    }

    public IAccount BurnAsset(
        IActionContext context, Address owner, FungibleAssetValue value)
    {
        // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
        // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

        var currency = value.Currency;

        if (value <= currency * 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        FungibleAssetValue balance = GetBalance(owner, currency);
        if (balance < value)
        {
            throw new InsufficientBalanceException(
                $"There is no sufficient balance for {owner}: {balance} < {value}",
                owner,
                value
            );
        }

        FungibleAssetValue nextValue = balance - value;
        return new AccountStateDelta(
            _states,
            _fungibles.SetItem(
                (owner, currency),
                nextValue.RawValue
            ),
            currency.TotalSupplyTrackable
                ? _totalSupplies.SetItem(
                    currency,
                    (GetTotalSupply(currency) - value).RawValue)
                : _totalSupplies,
            _validatorSet
        )
        {
            BaseState = BaseState,
        };
    }

    public ValidatorSet GetValidatorSet()
    {
        return _validatorSet ?? BaseState.GetValidatorSet();
    }

    public IAccount SetValidator(Validator validator)
    {
        return new AccountStateDelta(
            _states,
            _fungibles,
            _totalSupplies,
            GetValidatorSet().Update(validator)
        )
        {
            BaseState = BaseState,
        };
    }
}
