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
    [Obsolete("This class is reserved for backwards compatibility with MonsterCollection0 only.", false)]
    [Serializable]
    public class MonsterCollectionState0: State
    {
        public static Address DeriveAddress(Address baseAddress, int collectRound)
        {
            return baseAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    collectRound
                )
            );
        }

        public const string DeriveFormat = "monster-collection-{0}";
        public const long ExpirationIndex = RewardInterval * RewardCapacity;
        public const int RewardCapacity = 4;
        public const long RewardInterval = 50400;
        public const long LockUpInterval = 50400 * 4;

        public int Level { get; private set; }
        public long ExpiredBlockIndex { get; private set; }
        public long StartedBlockIndex { get; private set; }
        public long ReceivedBlockIndex { get; private set; }
        public long RewardLevel { get; private set; }
        public Dictionary<long, MonsterCollectionResult> RewardMap { get; private set; }
        public bool End { get; private set; }
        public Dictionary<long, List<MonsterCollectionRewardSheet.RewardInfo>> RewardLevelMap { get; private set; }

        public MonsterCollectionState0(Address address, int level, long blockIndex) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
        }

        public MonsterCollectionState0(Address address, int level, long blockIndex,
            MonsterCollectionRewardSheet monsterCollectionRewardSheet) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
            ExpiredBlockIndex = blockIndex + ExpirationIndex;
            RewardMap = new Dictionary<long, MonsterCollectionResult>();
            List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos = monsterCollectionRewardSheet[level].Rewards;
            RewardLevelMap = new Dictionary<long, List<MonsterCollectionRewardSheet.RewardInfo>>
            {
                [1] = rewardInfos,
                [2] = rewardInfos,
                [3] = rewardInfos,
                [4] = rewardInfos,
            };
        }

        public MonsterCollectionState0(Dictionary serialized) : base(serialized)
        {
            Level = serialized[LevelKey].ToInteger();
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ReceivedBlockIndex = serialized[ReceivedBlockIndexKey].ToLong();

            if (serialized.ContainsKey(ExpiredBlockIndexKey))
            {
                ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            }

            if (serialized.ContainsKey(RewardLevelKey))
            {
                RewardLevel = serialized[RewardLevelKey].ToLong();
            }

            if (serialized.ContainsKey(RewardMapKey))
            {
                RewardMap = ((Dictionary) serialized[RewardMapKey]).ToDictionary(
                    kv => kv.Key.ToLong(),
                    kv => new MonsterCollectionResult((Dictionary)kv.Value)
                );
            }

            if (serialized.ContainsKey(EndKey))
            {
                End = serialized[EndKey].ToBoolean();
            }

            if (serialized.ContainsKey(RewardLevelMapKey))
            {
                RewardLevelMap = ((Dictionary) serialized[RewardLevelMapKey])
                    .OrderBy(r => r.Key)
                    .ToDictionary(
                        kv => kv.Key.ToLong(),
                        kv => kv.Value
                            .ToList(v => new MonsterCollectionRewardSheet.RewardInfo((Dictionary)v))
                            .OrderBy(r => r.ItemId)
                            .ToList()
                    );
            }
        }

        public void Update(int level, long rewardLevel, MonsterCollectionRewardSheet monsterCollectionRewardSheet)
        {
            Level = level;
            List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos = monsterCollectionRewardSheet[level].Rewards;
            for (long i = rewardLevel; i < RewardLevelMap.Count; i++)
            {
                RewardLevelMap[i + 1] = rewardInfos;
            }
        }

        public void UpdateRewardMap(long rewardLevel, MonsterCollectionResult avatarAddress, long blockIndex)
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

        public long GetRewardLevel(long blockIndex)
        {
            long diff = Math.Max(0, blockIndex - StartedBlockIndex);
            return Math.Min(RewardCapacity, diff / RewardInterval);
        }

        public bool CanReceive(long blockIndex)
        {
            return blockIndex - Math.Max(StartedBlockIndex, ReceivedBlockIndex) >= RewardInterval;
        }

        public bool IsLock(long blockIndex)
        {
            return StartedBlockIndex +  LockUpInterval > blockIndex;
        }

        public void Receive(long blockIndex)
        {
            ReceivedBlockIndex = blockIndex;
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
                [(Text) RewardLevelMapKey] = new Dictionary(
                    RewardLevelMap.Select(
                        kv => new KeyValuePair<IKey, IValue>(
                            (IKey) kv.Key.Serialize(),
                            new List(kv.Value.Select(v => v.Serialize())).Serialize()
                        )
                    )
                ),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public override IValue SerializeV2()
        {
#pragma warning disable LAA1002
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) LevelKey] = Level.Serialize(),
                [(Text) StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text) ReceivedBlockIndexKey] = ReceivedBlockIndex.Serialize(),
            }.Union((Dictionary) base.SerializeV2()));
#pragma warning restore LAA1002
        }

        protected bool Equals(MonsterCollectionState0 other)
        {
#pragma warning disable LAA1002
            return Level == other.Level && ExpiredBlockIndex == other.ExpiredBlockIndex &&
                   StartedBlockIndex == other.StartedBlockIndex && ReceivedBlockIndex == other.ReceivedBlockIndex &&
                   RewardLevel == other.RewardLevel && RewardMap.SequenceEqual(other.RewardMap) && End == other.End &&
                   RewardLevelMap.SequenceEqual(other.RewardLevelMap);
#pragma warning restore LAA1002
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MonsterCollectionState0) obj);
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
                hashCode = (hashCode * 397) ^ (RewardLevelMap != null ? RewardLevelMap.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
