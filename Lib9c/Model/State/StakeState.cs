using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    public class StakeState : State
    {
        public class StakeAchievements
        {
            private readonly Dictionary<int, int> _achievements;

            public StakeAchievements(Dictionary<int, int> achievements = null)
            {
                _achievements = achievements ?? new Dictionary<int, int>();
            }

            public StakeAchievements(Dictionary serialized)
            {
                _achievements = serialized.ToDictionary(
                    pair => int.Parse(((Text)pair.Key).Value, CultureInfo.InvariantCulture),
                    pair => (int)(Integer)pair.Value);
            }

            public IValue Serialize() =>
                new Dictionary(_achievements.ToDictionary(
                    pair => (IKey)(Text)pair.Key.ToString(CultureInfo.InvariantCulture),
                    pair => (IValue)(Integer)pair.Value));

            public bool Check(int level, int step)
            {
                return _achievements.TryGetValue(level, out int achievedSteps) &&
                       achievedSteps >= step;
            }

            public void Achieve(int level, int step)
            {
                if (_achievements.ContainsKey(level))
                {
                    _achievements[level] = Math.Max(_achievements[level], step);
                }
                else
                {
                    _achievements[level] = step;
                }
            }
        }

        public const long RewardInterval = 50400;
        public const long LockupInterval = 50400 * 4;
        public const long StakeRewardSheetV2Index = 6_700_000L;

        public long CancellableBlockIndex { get; private set; }
        public long StartedBlockIndex { get; private set; }
        public long ReceivedBlockIndex { get; private set; }

        public StakeAchievements Achievements { get; private set; }

        public StakeState(Address address, long startedBlockIndex) : base(address)
        {
            StartedBlockIndex = startedBlockIndex;
            CancellableBlockIndex = startedBlockIndex + LockupInterval;
            Achievements = new StakeAchievements();
        }

        public StakeState(
            Address address,
            long startedBlockIndex,
            long receivedBlockIndex,
            long cancellableBlockIndex,
            StakeAchievements achievements
        ) : base(address)
        {
            StartedBlockIndex = startedBlockIndex;
            ReceivedBlockIndex = receivedBlockIndex;
            CancellableBlockIndex = cancellableBlockIndex;
            Achievements = achievements;
        }

        public StakeState(Dictionary serialized) : base(serialized)
        {
            CancellableBlockIndex = (long)serialized[CancellableBlockIndexKey].ToBigInteger();
            StartedBlockIndex = (long)serialized[StartedBlockIndexKey].ToBigInteger();
            ReceivedBlockIndex = (long)serialized[ReceivedBlockIndexKey].ToBigInteger();
            Achievements = new StakeAchievements((Dictionary)serialized[AchievementsKey]);
        }

        private Dictionary SerializeImpl()
        {
            return Dictionary.Empty.Add(CancellableBlockIndexKey, CancellableBlockIndex)
                .Add(StartedBlockIndexKey, StartedBlockIndex)
                .Add(ReceivedBlockIndexKey, ReceivedBlockIndex)
                .Add(AchievementsKey, Achievements.Serialize());
        }

        public override IValue Serialize() =>
            new Dictionary(SerializeImpl().Union((Dictionary)base.Serialize()));

        public override IValue SerializeV2() =>
            new Dictionary(SerializeImpl().Union((Dictionary)base.SerializeV2()));

        public bool IsCancellable(long blockIndex) => blockIndex >= CancellableBlockIndex;

        public bool IsClaimable(long blockIndex)
        {
            return IsClaimable(blockIndex, out _, out _);
        }

        public bool IsClaimable(long blockIndex, out int v1Step, out int v2Step)
        {
            if (blockIndex >= ActionObsoleteConfig.V100290ObsoleteIndex)
            {
                return CalculateAccumulatedRewards(blockIndex, out v1Step, out v2Step) > 0;
            }

            v1Step = 0;
            v2Step = 0;
            if (ReceivedBlockIndex == 0)
            {
                return StartedBlockIndex + RewardInterval <= blockIndex;
            }

            return ReceivedBlockIndex + RewardInterval <= blockIndex;
        }

        public void Claim(long blockIndex)
        {
            ReceivedBlockIndex = blockIndex;
        }

        public int CalculateAccumulatedRewards(long blockIndex)
        {
            return CalculateAccumulatedRewards(blockIndex, out _, out _);
        }

        public int CalculateAccumulatedRewards(long blockIndex, out int v1Step, out int v2Step)
        {
            return CalculateStep(blockIndex, StartedBlockIndex, out v1Step, out v2Step);
        }

        public int CalculateAccumulatedRuneRewards(long blockIndex)
        {
            return CalculateAccumulatedRuneRewards(blockIndex, out _, out _);
        }

        public int CalculateAccumulatedRuneRewards(long blockIndex, out int v1Step, out int v2Step)
        {
            long startedBlockIndex = Math.Max(StartedBlockIndex, ClaimStakeReward2.ObsoletedIndex);
            return CalculateStep(blockIndex, startedBlockIndex, out v1Step, out v2Step);
        }

        private int CalculateStep(long blockIndex, long startedBlockIndex, out int v1Step,
            out int v2Step)
        {
            int totalStep = (int)Math.DivRem(
                blockIndex - startedBlockIndex,
                RewardInterval,
                out _
            );

            int previousStep = 0;
            if (ReceivedBlockIndex > 0)
            {
                previousStep = (int)Math.DivRem(
                    ReceivedBlockIndex - startedBlockIndex,
                    RewardInterval,
                    out _
                );
            }

            v1Step = totalStep - previousStep;
            v2Step = 0;

            if (blockIndex >= StakeRewardSheetV2Index)
            {
                v1Step = Math.Max((int)Math.DivRem(
                    StakeRewardSheetV2Index -
                    Math.Max(ReceivedBlockIndex, startedBlockIndex),
                    RewardInterval,
                    out _
                ), 0);
                v2Step = totalStep - previousStep - v1Step;
            }

            return v1Step + v2Step;
        }

        public static Address DeriveAddress(Address agentAddress) => agentAddress.Derive("stake");
    }
}
