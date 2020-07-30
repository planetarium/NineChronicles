using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Serilog;
using Nekoyume.Model.State;
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
        public static readonly IValue MarkChanged = default(Null);

        // FIXME GoldCurrencyState 에 정의된 것과 다른데 괜찮을지 점검해봐야 합니다.
        protected static readonly Currency GoldCurrencyMock = new Currency();

        public abstract IValue PlainValue { get; }
        public abstract void LoadPlainValue(IValue plainValue);
        public abstract IAccountStateDelta Execute(IActionContext context);

        private struct AccountStateDelta : IAccountStateDelta
        {
            private IImmutableDictionary<Address, IValue> _states;
            private IImmutableDictionary<(Address, Currency), BigInteger> _balances;

            public IImmutableSet<Address> UpdatedAddresses => _states.Keys.ToImmutableHashSet();

            public IImmutableSet<Address> StateUpdatedAddresses => _states.Keys.ToImmutableHashSet();

            public IImmutableDictionary<Address, IImmutableSet<Currency>> UpdatedFungibleAssets =>
                _balances.GroupBy(kv => kv.Key.Item1).ToImmutableDictionary(
                    g => g.Key,
                    g => (IImmutableSet<Currency>)g.Select(kv => kv.Key.Item2).ToImmutableHashSet()
                );

            public AccountStateDelta(
                IImmutableDictionary<Address, IValue> states,
                IImmutableDictionary<(Address, Currency), BigInteger> balances
            )
            {
                _states = states;
                _balances = balances;
            }

            public AccountStateDelta(Dictionary states, List balances)
            {
                _states = states.ToImmutableDictionary(
                    kv => new Address(kv.Key.EncodeAsByteArray()),
                    kv => kv.Value
                );
                _balances = balances.Cast<Dictionary>().ToImmutableDictionary(
                    record => (record["address"].ToAddress(), CurrencyExtensions.Deserialize((Dictionary)record["currency"])),
                    record => record["amount"].ToBigInteger()
                );
            }

            public AccountStateDelta(IValue serialized)
                : this(
                    (Dictionary)((Dictionary)serialized)["states"],
                    (List)((Dictionary)serialized)["balances"]
                )
            {
            }

            public AccountStateDelta(byte[] bytes)
                : this((Dictionary)new Codec().Decode(bytes))
            {
            }

            public IValue GetState(Address address) =>
                _states.GetValueOrDefault(address, null);

            public IAccountStateDelta SetState(Address address, IValue state) =>
                new AccountStateDelta(_states.SetItem(address, state), _balances);

            public BigInteger GetBalance(Address address, Currency currency)
            {
                if (!_balances.TryGetValue((address, currency), out BigInteger balance))
                {
                    throw new BalanceDoesNotExistsException(address, currency);
                }

                return balance;
            }

            public IAccountStateDelta MintAsset(Address recipient, Currency currency, BigInteger amount)
            {
                // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
                // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

                if (amount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount));
                }

                return new AccountStateDelta(
                    _states,
                    _balances.SetItem((recipient, currency),
                        GetBalance(recipient, currency) + amount)
                );
            }

            public IAccountStateDelta TransferAsset(
                Address sender,
                Address recipient,
                Currency currency,
                BigInteger amount,
                bool allowNegativeBalance = false)
            {
                if (amount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount));
                }

                BigInteger senderBalance = GetBalance(sender, currency);
                if (senderBalance < amount)
                {
                    throw new InsufficientBalanceException(
                        sender,
                        currency,
                        senderBalance,
                        $"There is no sufficient balance for {sender}: {senderBalance} {currency} < {amount} {currency}"
                    );
                }

                var balances = _balances
                    .SetItem((sender, currency), senderBalance - amount)
                    .SetItem((recipient, currency), GetBalance(recipient, currency) + amount);
                return new AccountStateDelta(_states, balances);
            }

            public IAccountStateDelta BurnAsset(Address owner, Currency currency, BigInteger amount)
            {
                // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
                // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

                if (amount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount));
                }

                BigInteger balance = GetBalance(owner, currency);
                if (balance < amount)
                {
                    throw new InsufficientBalanceException(
                        owner,
                        currency,
                        balance,
                        $"There is no sufficient balance for {owner}: {balance} {currency} < {amount} {currency}"
                    );
                }

                return new AccountStateDelta(
                    _states,
                    _balances.SetItem((owner, currency), balance - amount)
                );
            }
        }

        [Serializable]
        public struct ActionEvaluation<T> : ISerializable
            where T : ActionBase
        {
            public T Action { get; set; }

            public Address Signer { get; set; }

            public long BlockIndex { get; set; }

            public IAccountStateDelta OutputStates { get; set; }

            public Exception Exception { get; set; }

            public IAccountStateDelta PreviousStates { get; set; }

            public ActionEvaluation(SerializationInfo info, StreamingContext ctx)
            {
                Action = FromBytes((byte[]) info.GetValue("action", typeof(byte[])));
                Signer = new Address((byte[]) info.GetValue("signer", typeof(byte[])));
                BlockIndex = info.GetInt64("blockIndex");
                OutputStates = new AccountStateDelta((byte[]) info.GetValue("outputStates", typeof(byte[])));
                Exception = (Exception) info.GetValue("exc", typeof(Exception));
                PreviousStates = new AccountStateDelta((byte[]) info.GetValue("previousStates", typeof(byte[])));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("action", ToBytes(Action));
                info.AddValue("signer", Signer.ToByteArray());
                info.AddValue("blockIndex", BlockIndex);
                info.AddValue("outputStates", ToBytes(OutputStates, OutputStates.UpdatedAddresses));
                info.AddValue("exc", Exception);
                info.AddValue("previousStates", ToBytes(PreviousStates, OutputStates.UpdatedAddresses));
            }

            private static byte[] ToBytes(T action)
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, action);
                    return stream.ToArray();
                }
            }

            private static byte[] ToBytes(IAccountStateDelta delta, IImmutableSet<Address> updatedAddresses)
            {
                var state = new Dictionary(
                    updatedAddresses.Select(addr => new KeyValuePair<IKey, IValue>(
                        (Binary) addr.ToByteArray(),
                        delta.GetState(addr) ?? new Bencodex.Types.Null()
                    ))
                );
                var balance = new Bencodex.Types.List(
                    delta.UpdatedFungibleAssets.SelectMany(ua =>
                        ua.Value.Select(c =>
                            new Bencodex.Types.Dictionary(new []
                            {
                                new KeyValuePair<IKey, IValue>((Text) "address", (Binary) ua.Key.ToByteArray()),
                                new KeyValuePair<IKey, IValue>((Text) "currency", c.Serialize()),
                                new KeyValuePair<IKey, IValue>((Text) "amount", delta.GetBalance(ua.Key, c).Serialize()),
                            })
                        )
                    ).Cast<IValue>()
                );

                var bdict = new Dictionary(new []
                {
                    new KeyValuePair<IKey, IValue>((Text) "states", state),
                    new KeyValuePair<IKey, IValue>((Text) "balances", balance),
                });

                return new Codec().Encode(bdict);
            }

            private static T FromBytes(byte[] bytes)
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream(bytes))
                {
                    return (T)formatter.Deserialize(stream);
                }
            }
        }

        public static readonly Subject<ActionEvaluation<ActionBase>> RenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        public static readonly Subject<ActionEvaluation<ActionBase>> UnrenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
            RenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
            });
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
            UnrenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
            });
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

        public void RenderError(IActionContext context, Exception exception)
        {
            RenderSubject.OnNext(
                new ActionEvaluation<ActionBase>()
                {
                    Action = this,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = context.PreviousStates,
                    Exception = exception,
                    PreviousStates = context.PreviousStates,
                }
            );
        }

        public void UnrenderError(IActionContext context, Exception exception)
        {
            UnrenderSubject.OnNext(
                new ActionEvaluation<ActionBase>()
                {
                    Action = this,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = context.PreviousStates,
                    Exception = exception,
                    PreviousStates = context.PreviousStates,
                }
            );
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
    }
}
