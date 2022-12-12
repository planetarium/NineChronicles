using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Linq;
using System.Numerics;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Serilog;
using Nekoyume.Model.State;
using Libplanet.Assets;
using Libplanet.Tx;
using Nekoyume.BlockChain.Policy;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class ActionBase : IAction
    {
        public static readonly IValue MarkChanged = Null.Value;

        // FIXME GoldCurrencyState 에 정의된 것과 다른데 괜찮을지 점검해봐야 합니다.
        protected static readonly Currency GoldCurrencyMock = new Currency();

        public abstract IValue PlainValue { get; }
        public abstract void LoadPlainValue(IValue plainValue);
        public abstract IAccountStateDelta Execute(IActionContext context);

        public struct AccountStateDelta : IAccountStateDelta
        {
            private IImmutableDictionary<Address, IValue> _states;
            private IImmutableDictionary<(Address, Currency), BigInteger> _balances;
            private IImmutableDictionary<Currency, BigInteger> _totalSupplies;

            public IImmutableSet<Address> UpdatedAddresses => _states.Keys.ToImmutableHashSet();

            public IImmutableSet<Address> StateUpdatedAddresses => _states.Keys.ToImmutableHashSet();

#pragma warning disable LAA1002
            public IImmutableDictionary<Address, IImmutableSet<Currency>> UpdatedFungibleAssets =>
                _balances.GroupBy(kv => kv.Key.Item1).ToImmutableDictionary(
                    g => g.Key,
                    g => (IImmutableSet<Currency>)g.Select(kv => kv.Key.Item2).ToImmutableHashSet()
                );
#pragma warning restore LAA1002

            public IImmutableSet<Currency> TotalSupplyUpdatedCurrencies =>
                _totalSupplies.Keys.ToImmutableHashSet();

            public AccountStateDelta(
                IImmutableDictionary<Address, IValue> states,
                IImmutableDictionary<(Address, Currency), BigInteger> balances,
                IImmutableDictionary<Currency, BigInteger> totalSupplies
            )
            {
                _states = states;
                _balances = balances;
                _totalSupplies = totalSupplies;
            }

            public AccountStateDelta(Dictionary states, List balances, Dictionary totalSupplies)
            {
                // This assumes `states` consists of only Binary keys:
                _states = states.ToImmutableDictionary(
                    kv => new Address((Binary)kv.Key),
                    kv => kv.Value
                );

                _balances = balances.Cast<Dictionary>().ToImmutableDictionary(
                    record => (record["address"].ToAddress(), CurrencyExtensions.Deserialize((Dictionary)record["currency"])),
                    record => record["amount"].ToBigInteger()
                );

                // This assumes `totalSupplies` consists of only Binary keys:
                _totalSupplies = totalSupplies.ToImmutableDictionary(
                    kv => CurrencyExtensions.Deserialize((Dictionary)((Binary)kv.Key as IValue)),
                    kv => kv.Value.ToBigInteger()
                );
            }

            public AccountStateDelta(IValue serialized)
                : this(
                    (Dictionary)((Dictionary)serialized)["states"],
                    (List)((Dictionary)serialized)["balances"],
                    (Dictionary)((Dictionary)serialized)["totalSupplies"]
                )
            {
            }

            public AccountStateDelta(byte[] bytes)
                : this((Dictionary)new Codec().Decode(bytes))
            {
            }

            public IValue GetState(Address address) =>
                _states.GetValueOrDefault(address, null);

            public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
                addresses.Select(_states.GetValueOrDefault).ToArray();

            public IAccountStateDelta SetState(Address address, IValue state) =>
                new AccountStateDelta(_states.SetItem(address, state), _balances, _totalSupplies);

            public FungibleAssetValue GetBalance(Address address, Currency currency)
            {
                if (!_balances.TryGetValue((address, currency), out BigInteger rawValue))
                {
                    throw new BalanceDoesNotExistsException(address, currency);
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

                throw new TotalSupplyDoesNotExistException(currency);
            }

            public IAccountStateDelta MintAsset(Address recipient, FungibleAssetValue value)
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
                        _balances.SetItem(
                            (recipient, value.Currency),
                            nextAmount.RawValue
                        ),
                        _totalSupplies.SetItem(currency, (currentTotalSupply + value).RawValue)
                    );
                }

                return new AccountStateDelta(
                    _states,
                    _balances.SetItem(
                        (recipient, value.Currency),
                        nextAmount.RawValue
                    ),
                    _totalSupplies
                );
            }

            public IAccountStateDelta TransferAsset(
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
                var balances = _balances
                    .SetItem((sender, currency), senderRemains.RawValue)
                    .SetItem((recipient, currency), recipientRemains.RawValue);
                return new AccountStateDelta(_states, balances, _totalSupplies);
            }

            public IAccountStateDelta BurnAsset(Address owner, FungibleAssetValue value)
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
                    _balances.SetItem(
                        (owner, currency),
                        nextValue.RawValue
                    ),
                    currency.TotalSupplyTrackable
                        ? _totalSupplies.SetItem(
                            currency,
                            (GetTotalSupply(currency) - value).RawValue)
                        : _totalSupplies
                );
            }
        }

        public struct ActionEvaluation<T>
            where T : ActionBase
        {
            public T Action { get; set; }

            public Address Signer { get; set; }

            public long BlockIndex { get; set; }

            public TxId? TxId { get; set; }

            public IAccountStateDelta OutputStates { get; set; }

            public Exception Exception { get; set; }

            public IAccountStateDelta PreviousStates { get; set; }

            public int RandomSeed { get; set; }

            public Dictionary<string, IValue> Extra { get; set; }
        }

        /// <summary>
        /// returns "[Signer Address, AvatarState Address, ...]"
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="addresses"></param>
        /// <returns></returns>
        protected string GetSignerAndOtherAddressesHex(IActionContext ctx, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{ctx.Signer.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        protected IAccountStateDelta LogError(IActionContext context, string message, params object[] values)
        {
            string actionType = GetType().Name;
            object[] prependedValues = new object[values.Length + 2];
            prependedValues[0] = context.BlockIndex;
            prependedValues[1] = context.Signer;
            values.CopyTo(prependedValues, 2);
            string msg = $"#{{BlockIndex}} {actionType} (by {{Signer}}): {message}";
            Log.Error(msg, prependedValues);
            return context.PreviousStates;
        }

        protected bool TryGetAdminState(IActionContext ctx, out AdminState state)
        {
            state = default;

            IValue rawState = ctx.PreviousStates.GetState(AdminState.Address);
            if (rawState is Bencodex.Types.Dictionary asDict)
            {
                state = new AdminState(asDict);
                return true;
            }

            return false;
        }

        protected void CheckPermission(IActionContext ctx)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            return;
#endif
            if (TryGetAdminState(ctx, out AdminState policy))
            {
                if (ctx.BlockIndex > policy.ValidUntil)
                {
                    throw new PolicyExpiredException(policy, ctx.BlockIndex);
                }

                if (policy.AdminAddress != ctx.Signer)
                {
                    throw new PermissionDeniedException(policy, ctx.Signer);
                }
            }
        }

        protected void CheckObsolete(long obsoleteIndex, IActionContext ctx)
        {
            if (ctx.BlockIndex > obsoleteIndex)
            {
                throw new ActionObsoletedException();
            }
        }

        protected bool UseV100291Sheets(long blockIndex)
        {
            return blockIndex < BlockPolicySource.V100301ExecutedBlockIndex;
        }

        protected void CheckActionAvailable(long startedIndex, IActionContext ctx)
        {
            if (ctx.BlockIndex <= startedIndex)
            {
                throw new ActionUnavailableException();
            }
        }
    }
}
