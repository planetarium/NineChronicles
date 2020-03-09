using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using Bencodex;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public abstract IValue PlainValue { get; }
        public abstract void LoadPlainValue(IValue plainValue);
        public abstract IAccountStateDelta Execute(IActionContext context);

        private struct AccountStateDelta : IAccountStateDelta
        {
            private IImmutableDictionary<Address, IValue> _states;
            
            public IImmutableSet<Address> UpdatedAddresses => _states.Keys.ToImmutableHashSet();

            public AccountStateDelta(IImmutableDictionary<Address, IValue> states)
            {
                _states = states;
            }

            public AccountStateDelta(Dictionary states)
            {
                _states = states.ToImmutableDictionary(
                    kv => new Address(kv.Key.EncodeAsByteArray()),
                    kv => kv.Value
                );
            }

            public AccountStateDelta(byte[] bytes) 
                : this((Dictionary)new Codec().Decode(bytes))
            {
            }

            public IValue GetState(Address address)
            {
                return _states.GetValueOrDefault(address, new Null());
            }

            public IAccountStateDelta SetState(Address address, IValue state)
            {
                return new AccountStateDelta(_states.Add(address, state));
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

            public ActionEvaluation(SerializationInfo info, StreamingContext ctx)
            {
                Action = FromBytes((byte[])info.GetValue("action", typeof(byte[])));
                Signer = new Address((byte[])info.GetValue("signer", typeof(byte[])));
                BlockIndex = info.GetInt64("blockIndex");
                OutputStates = new AccountStateDelta((byte[])info.GetValue("outputStates", typeof(byte[])));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("action", ToBytes(Action));
                info.AddValue("signer", Signer.ToByteArray());
                info.AddValue("blockIndex", BlockIndex);
                info.AddValue("outputStates", ToBytes(OutputStates));
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

            private static byte[] ToBytes(IAccountStateDelta delta)
            {
                var bdict = new Dictionary(
                    delta.UpdatedAddresses.Select(addr =>
                    {
                        return new KeyValuePair<IKey, IValue>(
                            (Binary) addr.ToByteArray(), 
                            delta.GetState(addr));
                    })
                );
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
            });
        }
    }
}
