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
    public class MonsterCollectionState: State
    {
        // We need `round` to integrate previous states.
        public static Address DeriveAddress(Address baseAddress, int round)
        {
            return baseAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    round
                )
            );
        }

        public const string DeriveFormat = "monster-collection-{0}";
        public const long RewardInterval = 50400;
        public const long LockUpInterval = 50400 * 4;

        public int Level { get; private set; }
        public long ExpiredBlockIndex { get; private set; }
        public long StartedBlockIndex { get; private set; }
        public long ReceivedBlockIndex { get; private set; }
        public long RewardLevel { get; private set; }

        public MonsterCollectionState(Address address, int level, long blockIndex) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
        }

        public MonsterCollectionState(Address address, int level, long blockIndex,
            MonsterCollectionRewardSheet monsterCollectionRewardSheet) : base(address)
        {
            Level = level;
            StartedBlockIndex = blockIndex;
            ExpiredBlockIndex = blockIndex + LockUpInterval;
            List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos = monsterCollectionRewardSheet[level].Rewards;
        }

        public MonsterCollectionState(Dictionary serialized) : base(serialized)
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
        }

        public bool IsLocked(long blockIndex)
        {
            return StartedBlockIndex +  LockUpInterval > blockIndex;
        }

        public void Claim(long blockIndex)
        {
            ReceivedBlockIndex = blockIndex;
        }

        public int CalculateStep(long blockIndex)
        {
            int step = (int)Math.DivRem(
                blockIndex - StartedBlockIndex,
                RewardInterval,
                out _
            );
            if (ReceivedBlockIndex > 0)
            {
                int previousStep = (int)Math.DivRem(
                    ReceivedBlockIndex - StartedBlockIndex,
                    RewardInterval,
                    out _
                );
                step -= previousStep;
            }

            return step;
        }

        public List<MonsterCollectionRewardSheet.RewardInfo> CalculateRewards(
            MonsterCollectionRewardSheet sheet,
            long blockIndex
        )
        {
            int step = CalculateStep(blockIndex);
            if (step > 0)
            {
                return sheet[Level].Rewards
                    .GroupBy(ri => ri.ItemId)
                    .Select(g => new MonsterCollectionRewardSheet.RewardInfo(
                                g.Key,
                                g.Sum(ri => ri.Quantity) * step))
                    .ToList();
            }
            else
            {
                return new List<MonsterCollectionRewardSheet.RewardInfo>();
            }
        }

        public override IValue Serialize()
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

        protected bool Equals(MonsterCollectionState other)
        {
#pragma warning disable LAA1002
            return Level == other.Level && ExpiredBlockIndex == other.ExpiredBlockIndex &&
                   StartedBlockIndex == other.StartedBlockIndex && ReceivedBlockIndex == other.ReceivedBlockIndex &&
                   RewardLevel == other.RewardLevel;
#pragma warning restore LAA1002
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MonsterCollectionState) obj);
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
                return hashCode;
            }
        }
    }
}
