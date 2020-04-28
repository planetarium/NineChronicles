using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class PromotionCodeState : State
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x5
            }
        );

        public IReadOnlyDictionary<Address, Reward> Map => _map;

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

        private Dictionary<Address, Reward> _map = new Dictionary<Address,Reward>();

        public PromotionCodeState() : base(Address)
        {
            //TODO PrivateKey 목록 생성해서 제네시스 세일전에 반영.
            _map[Address] = new Reward(400000);
        }

        public PromotionCodeState(Dictionary serialized) : base(serialized)
        {
            _map = ((Dictionary) serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new Reward((Dictionary) kv.Value)
            );
        }

        public PromotionCodeState(IValue iValue) : base(iValue)
        {
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "map"] = new Dictionary(_map.Select(kv => new KeyValuePair<IKey, IValue>(
                    (Binary) kv.Key.Serialize(),
                    kv.Value.Serialize()
                )))
            }.Union((Dictionary) base.Serialize()));

        public int Redeem(Address key, Address userAddress)
        {
            if (!_map.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            var result = _map[key];
            if (result.UserAddress.HasValue)
            {
                throw new InvalidOperationException($"Code already used by {result.UserAddress}");
            }

            result.UserAddress = userAddress;
            _map[key] = result;
            return result.RewardId;
        }
    }
}
