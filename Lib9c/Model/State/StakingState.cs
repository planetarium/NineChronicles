using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class StakingState: State
    {
        [Serializable]
        public class Result
        {
            public Address avatarAddress;
            public List<StakingRewardSheet.RewardInfo> rewards;

            public Result(Address address, List<StakingRewardSheet.RewardInfo> rewardInfos)
            {
                avatarAddress = address;
                rewards = rewardInfos;
            }

            public Result(Dictionary serialized)
            {
                avatarAddress = serialized[AvatarAddressKey].ToAddress();
                rewards = serialized[StakingResultKey]
                    .ToList(s => new StakingRewardSheet.RewardInfo((Dictionary) s));
            }

            public Dictionary Serialize() =>
                Dictionary.Empty
                    .Add(AvatarAddressKey, avatarAddress.Serialize())
                    .Add(StakingResultKey, new List(rewards.Select(r => r.Serialize())).Serialize());
        }

        public static Address DeriveAddress(Address baseAddress, int stakingRound)
        {
            return baseAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    stakingRound
                )
            );
        }

        public const string DeriveFormat = "staking-{0}";
        public const long ExpirationIndex = RewardInterval * RewardCapacity;
        public const int RewardCapacity = 4;
        public const long RewardInterval = 40000;

        public int Level { get; private set; }
        public long ExpiredBlockIndex { get; private set; }
        public long StartedBlockIndex { get; private set; }
        public long ReceivedBlockIndex { get; private set; }
        public long RewardLevel { get; private set; }
        public Dictionary<long, Result> RewardMap { get; private set; }
        public bool End { get; private set; }

        public StakingState(Address address, int level, long blockIndex) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
            ExpiredBlockIndex = blockIndex + ExpirationIndex;
            RewardMap = new Dictionary<long, Result>();
        }

        public StakingState(Dictionary serialized) : base(serialized)
        {
            Level = serialized[LevelKey].ToInteger();
            ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ReceivedBlockIndex = serialized[ReceivedBlockIndexKey].ToLong();
            RewardLevel = serialized[RewardLevelKey].ToLong();
            RewardMap = ((Dictionary) serialized[RewardMapKey]).ToDictionary(
                kv => kv.Key.ToLong(),
                kv => new Result((Dictionary)kv.Value)
            );
            End = serialized[EndKey].ToBoolean();
        }

        public void Update(int level)
        {
            Level = level;
        }

        public void UpdateRewardMap(long rewardLevel, Result avatarAddress, long blockIndex)
        {
            if (rewardLevel < 0 || rewardLevel > RewardCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardLevel),
                    $"reward level must be greater than 0 and less than {RewardCapacity}.");
            }

            if (RewardMap.ContainsKey(rewardLevel))
            {
                throw new AlreadyReceivedException("");
            }

            RewardMap[rewardLevel] = avatarAddress;
            RewardLevel = rewardLevel;
            ReceivedBlockIndex = blockIndex;
            End = rewardLevel == 4;
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) LevelKey] = Level.Serialize(),
                [(Text) ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
                [(Text) StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text) ReceivedBlockIndexKey] = ReceivedBlockIndex.Serialize(),
                [(Text) RewardLevelKey] = RewardLevel.Serialize(),
                [(Text) RewardMapKey] = new Dictionary(
                    RewardMap.Select(
                        kv => new KeyValuePair<IKey, IValue>(
                            (IKey) kv.Key.Serialize(),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text) EndKey] = End.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        protected bool Equals(StakingState other)
        {
#pragma warning disable LAA1002
            return Level == other.Level && ExpiredBlockIndex == other.ExpiredBlockIndex &&
                   StartedBlockIndex == other.StartedBlockIndex && ReceivedBlockIndex == other.ReceivedBlockIndex &&
                   RewardLevel == other.RewardLevel && RewardMap.SequenceEqual(other.RewardMap) && End == other.End;
#pragma warning restore LAA1002
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StakingState) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Level;
                hashCode = (hashCode * 397) ^ ExpiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ RewardLevel.GetHashCode();
                hashCode = (hashCode * 397) ^ (RewardMap != null ? RewardMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                return hashCode;
            }
        }
    }
}
