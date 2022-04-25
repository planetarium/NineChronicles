using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    public class StakeState : State
    {
        public class StakeAchievements
        {
            private readonly Dictionary<int, int[]> _achievements;

            public StakeAchievements(Dictionary<int, int[]> achievements = null)
            {
                _achievements = achievements ?? new Dictionary<int, int[]>();
            }

            public StakeAchievements(Dictionary serialized)
            {
                _achievements = serialized.ToDictionary(
                    pair => int.Parse(((Text) pair.Key).Value, CultureInfo.InvariantCulture),
                    pair => ((List) pair.Value).Cast<Integer>().Select(x => (int)x.Value).ToArray());
            }

            public IValue Serialize() =>
                new Dictionary(_achievements.OrderBy(pair => pair.Key).Select(pair =>
                    new KeyValuePair<IKey, IValue>(
                        (Text) pair.Key.ToString(CultureInfo.InvariantCulture),
                        new List(pair.Value.Select(x => (IValue) (Integer) x)))));

            public bool Check(int level, int step)
            {
                return _achievements.TryGetValue(level, out int[] achievedSteps) &&
                       achievedSteps.Length > step;
            }
        }

        public const long RewardInterval = 50400;
        public const long LockupInterval = 50400 * 4;

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
            new Dictionary(SerializeImpl().Union((Dictionary) base.Serialize()));

        public override IValue SerializeV2() =>
            new Dictionary(SerializeImpl().Union((Dictionary) base.SerializeV2()));

        public bool IsCancellable(long blockIndex) => blockIndex >= CancellableBlockIndex;

        public static Address DeriveAddress(Address agentAddress) => agentAddress.Derive("stake");
    }
}
