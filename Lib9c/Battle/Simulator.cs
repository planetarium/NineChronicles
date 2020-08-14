using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public abstract class Simulator
    {
        public readonly IRandom Random;
        public readonly BattleLog Log;
        public readonly Player Player;
        public BattleLog.Result Result { get; protected set; }
        public SimplePriorityQueue<CharacterBase, decimal> Characters;
        public const decimal TurnPriority = 100m;
        public readonly TableSheets TableSheets;
        protected const int MaxTurn = 200;
        public int TurnNumber;
        public int WaveNumber;
        public int WaveTurn;
        public abstract IEnumerable<ItemBase> Reward { get; }

        protected Simulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            TableSheets tableSheets)
        {
            Random = random;
            TableSheets = tableSheets;
            Log = new BattleLog();
            Player = new Player(avatarState, this);
            Player.Use(foods);
            Player.Stats.EqualizeCurrentHPWithHP();
        }

        public abstract Player Simulate();

        public static List<ItemBase> SetReward(
            WeightedSelector<StageSheet.RewardData> itemSelector,
            int maxCount,
            IRandom random,
            TableSheets tableSheets
        )
        {
            var reward = new List<ItemBase>();

            while (reward.Count < maxCount)
            {
                try
                {
                    var data = itemSelector.Select(1).First();
                    if (tableSheets.MaterialItemSheet.TryGetValue(data.ItemId, out var itemData))
                    {
                        var count = random.Next(data.Min, data.Max + 1);
                        for (var i = 0; i < count; i++)
                        {
                            var item = ItemFactory.CreateMaterial(itemData);
                            if (reward.Count < maxCount)
                            {
                                reward.Add(item);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (ListEmptyException)
                {
                    break;
                }
            }

            return reward;
        }

    }
}
