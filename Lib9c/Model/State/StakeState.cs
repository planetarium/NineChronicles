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
        public const long CurrencyAsRewardStartIndex = 6_910_000L;

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
                return CalculateAccumulatedItemRewards(blockIndex, out v1Step, out v2Step) > 0;
            }

            v1Step = 0;
            v2Step = 0;
            if (ReceivedBlockIndex == 0)
            {
                return StartedBlockIndex + RewardInterval <= blockIndex;
            }

            // FIXME:
            // The ReceivedBlockIndex is not a baseline block index for the stake reward.
            // It is the block index when the stake reward was received.
            // So, we need to calculate the block index for the stake reward and use it
            // instead of the ReceivedBlockIndex.
            return ReceivedBlockIndex + RewardInterval <= blockIndex;
        }

        public void Claim(long blockIndex)
        {
            ReceivedBlockIndex = blockIndex;
        }

        public int CalculateAccumulatedItemRewards(long blockIndex)
        {
            return CalculateAccumulatedItemRewards(blockIndex, out _, out _);
        }

        public int CalculateAccumulatedItemRewards(long blockIndex, out int v1Step, out int v2Step)
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

        public int CalculateAccumulatedCurrencyRewards(long blockIndex, out int v2Step)
        {
            v2Step = GetRewardStep(blockIndex, CurrencyAsRewardStartIndex);
            return v2Step;
        }

        /// <summary>
        /// Calculate accumulated rewards step.
        /// </summary>
        /// <param name="currentBlockIndex">The block index of the current block.</param>
        /// <param name="rewardStartBlockIndex">
        /// The block index of the reward start block.
        /// If not null, the return value is calculated differently like below.
        /// # Case 1
        ///   RewardInterval: 10, StartedBlockIndex: 0, ReceivedBlockIndex: 0,
        ///   currentBlockIndex: 100, rewardStartBlockIndex: null
        ///   -> 10
        /// # Case 2
        ///   RewardInterval: 10, StartedBlockIndex: 0, ReceivedBlockIndex: 15,
        ///   currentBlockIndex: 100, rewardStartBlockIndex: null
        ///   -> 9
        /// # Case 3
        ///   RewardInterval: 10, StartedBlockIndex: 0, ReceivedBlockIndex: 0,
        ///   currentBlockIndex: 100, rewardStartBlockIndex: 15
        ///   -> 8
        /// # Case 4
        ///   RewardInterval: 10, StartedBlockIndex: 0, ReceivedBlockIndex: 55,
        ///   currentBlockIndex: 100, rewardStartBlockIndex: 15
        ///   -> 5
        /// # Case 5
        ///   RewardInterval: 10, StartedBlockIndex: 0, ReceivedBlockIndex: 15,
        ///   currentBlockIndex: 100, rewardStartBlockIndex: 55
        ///   -> 4
        /// # Case 6
        ///   RewardInterval: 10, StartedBlockIndex: 100, ReceivedBlockIndex: 0,
        ///   currentBlockIndex: 200, rewardStartBlockIndex: 55
        ///   -> 10
        /// # Case 7
        ///   RewardInterval: 10, StartedBlockIndex: 100, ReceivedBlockIndex: 115,
        ///   currentBlockIndex: 200, rewardStartBlockIndex: 55
        ///   -> 9
        /// </param>
        /// <returns>The accumulated rewards step.</returns>
        internal int GetRewardStep(long currentBlockIndex, long? rewardStartBlockIndex)
        {
            var stepBlockIndexes = new List<long>();
            while (true)
            {
                var nextStepBlockIndex =
                    StartedBlockIndex + RewardInterval * (stepBlockIndexes.Count + 1);
                if (nextStepBlockIndex > currentBlockIndex)
                {
                    break;
                }

                stepBlockIndexes.Add(nextStepBlockIndex);
            }

            if (stepBlockIndexes.Count == 0)
            {
                return 0;
            }

            if (ReceivedBlockIndex > 0)
            {
                stepBlockIndexes = stepBlockIndexes
                    .Where(stepBlockIndex => stepBlockIndex > ReceivedBlockIndex)
                    .ToList();
                if (stepBlockIndexes.Count == 0)
                {
                    return 0;
                }
            }

            if (rewardStartBlockIndex.HasValue)
            {
                var validStepBlockIndex = rewardStartBlockIndex + RewardInterval;
                stepBlockIndexes = stepBlockIndexes
                    .Where(stepBlockIndex => stepBlockIndex >= validStepBlockIndex)
                    .ToList();
            }

            return stepBlockIndexes.Count;
        }

        private int CalculateStep(
            long blockIndex,
            long startedBlockIndex,
            out int v1Step,
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
                // NOTE: The previousStep can be negative
                // if startedBlockIndex is greater than ReceivedBlockIndex property.
                // The startedBlockIndex is argument of this method and
                // it is not same as StartedBlockIndex property.
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
