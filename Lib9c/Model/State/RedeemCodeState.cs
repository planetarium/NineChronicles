using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class RedeemCodeState : State
    {
        public static readonly Address Address = Addresses.RedeemCode;
        public IReadOnlyDictionary<PublicKey, Reward> Map =>
            _mapProxy ?? (_mapProxy = new RedeemCodeMap(_map));

        public class Reward
        {
            public Address? UserAddress;
            public readonly int RewardId;

            public Reward(int rewardId)
            {
                RewardId = rewardId;
            }

            public Reward(Dictionary serialized)
            {
                if (serialized.TryGetValue((Text) "userAddress", out var ua))
                {
                    UserAddress = ua.ToAddress();
                }
                RewardId = serialized["rewardId"].ToInteger();
            }

            public IValue Serialize()
            {
                var values = new Dictionary<IKey, IValue>
                {
                    [(Text) "rewardId"] = RewardId.Serialize(),
                };
                if (UserAddress.HasValue)
                {
                    values.Add((Text) "userAddress", UserAddress.Serialize());
                }

                return new Dictionary(values);
            }
        }

        private readonly Dictionary<Binary, Reward> _map = new Dictionary<Binary, Reward>();
        private RedeemCodeMap _mapProxy;

        public RedeemCodeState(RedeemCodeListSheet sheet) : base(Address)
        {
            //TODO 프라이빗키 목록을 받아서 주소대신 퍼블릭키를 키로 써야함.
            foreach (var row in sheet.Values)
            {
                _map[row.PublicKeyBinary] = new Reward(row.RewardId);
            }
        }

        public RedeemCodeState(Dictionary serialized)
            : base(Address)
        {
            _map = ((Dictionary) serialized["map"]).ToDictionary(
                kv => (Binary) kv.Key,
                kv => new Reward((Dictionary) kv.Value)
            );
        }

        public RedeemCodeState(Dictionary<PublicKey, Reward> rewardMap)
            : base(Address)
        {
            _map = rewardMap.ToDictionary(kv => (Binary)kv.Key.Format(true), kv => kv.Value);
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "map"] = new Dictionary(_map.Select(kv => new KeyValuePair<IKey, IValue>(
                    kv.Key,
                    kv.Value.Serialize()
                )))
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        public int Redeem(string code, Address userAddress)
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(code));
            PublicKey publicKey = privateKey.PublicKey;

            if (!Map.ContainsKey(publicKey))
            {
                throw new InvalidRedeemCodeException();
            }

            var result = Map[publicKey];
            if (result.UserAddress.HasValue)
            {
                throw new DuplicateRedeemException($"Code already used by {result.UserAddress}");
            }

            result.UserAddress = userAddress;
            _map[publicKey.Format(true)] = result;
            return result.RewardId;
        }

        public void Update(RedeemCodeListSheet sheet)
        {
            foreach (var row in sheet.OrderedList)
            {
                if (!Map.ContainsKey(row.PublicKey))
                {
                    _map[row.PublicKey.Format(true)] = new Reward(row.RewardId);
                }
                else
                {
                    throw new SheetRowValidateException($"{nameof(RedeemCodeState)} already contains {row.PublicKey}");
                }
            }
        }
    }

    [Serializable]
    public class InvalidRedeemCodeException : KeyNotFoundException
    {
        public InvalidRedeemCodeException()
        {
        }

        public InvalidRedeemCodeException(string s) : base(s)
        {
        }

        public InvalidRedeemCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class DuplicateRedeemException : InvalidOperationException
    {
        public DuplicateRedeemException(string s) : base(s)
        {
        }

        public DuplicateRedeemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    internal class RedeemCodeMap : IReadOnlyDictionary<PublicKey, RedeemCodeState.Reward>
    {
        private IReadOnlyDictionary<Binary, RedeemCodeState.Reward> _map;

        public RedeemCodeMap(IReadOnlyDictionary<Binary, RedeemCodeState.Reward> map) =>
            _map = map;

        public IEnumerator<KeyValuePair<PublicKey, RedeemCodeState.Reward>> GetEnumerator()
        {
            foreach (KeyValuePair<Binary, RedeemCodeState.Reward> kv in _map)
            {
                yield return new KeyValuePair<PublicKey, RedeemCodeState.Reward>(
                    kv.Key.ToPublicKey(),
                    kv.Value
                );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _map).GetEnumerator();

        public int Count => _map.Count;

        public bool ContainsKey(PublicKey key) =>
            _map.ContainsKey(key.Format(true)) ||
            _map.ContainsKey(key.Format(false));

        public bool TryGetValue(PublicKey key, out RedeemCodeState.Reward value) =>
            _map.TryGetValue(key.Format(true), out value) ||
            _map.TryGetValue(key.Format(false), out value);

        public RedeemCodeState.Reward this[PublicKey key]
        {
            get
            {
                try
                {
                    return _map[key.Format(true)];
                }
                catch (KeyNotFoundException)
                {
                    return _map[key.Format(false)];
                }
            }
        }

        public IEnumerable<PublicKey> Keys => _map.Keys.Select(k => k.ToPublicKey());

        public IEnumerable<RedeemCodeState.Reward> Values => _map.Values;
    }
}
