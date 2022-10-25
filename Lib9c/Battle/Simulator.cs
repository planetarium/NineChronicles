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
    public abstract class Simulator : ISimulator
    {
        public readonly IRandom Random;
        public BattleLog Log { get; }
        public Player Player { get; }
        public BattleLog.Result Result { get; protected set; }
        public SimplePriorityQueue<CharacterBase, decimal> Characters { get; set; }
        public const decimal TurnPriority = 100m;
        public readonly MaterialItemSheet MaterialItemSheet;
        public readonly SkillSheet SkillSheet;
        public readonly SkillBuffSheet SkillBuffSheet;
        public readonly StatBuffSheet StatBuffSheet;
        public readonly SkillActionBuffSheet SkillActionBuffSheet;
        public readonly ActionBuffSheet ActionBuffSheet;
        public readonly CharacterSheet CharacterSheet;
        public readonly CharacterLevelSheet CharacterLevelSheet;
        public readonly EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet;
        protected const int MaxTurn = 200;
        public int TurnNumber;
        public int WaveNumber { get; protected set; }
        public int WaveTurn { get; set; }
        public abstract IEnumerable<ItemBase> Reward { get; }

        protected Simulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            SimulatorSheetsV1 simulatorSheets
        ) : this(random, new Player(avatarState, simulatorSheets), foods, simulatorSheets)
        {
        }

        protected Simulator(
            IRandom random,
            Player player,
            List<Guid> foods,
            SimulatorSheetsV1 simulatorSheets
        )
        {
            Random = random;
            MaterialItemSheet = simulatorSheets.MaterialItemSheet;
            SkillSheet = simulatorSheets.SkillSheet;
            SkillBuffSheet = simulatorSheets.SkillBuffSheet;
            StatBuffSheet = simulatorSheets.StatBuffSheet;
            SkillActionBuffSheet = simulatorSheets.SkillActionBuffSheet;
            ActionBuffSheet = simulatorSheets.ActionBuffSheet;
            CharacterSheet = simulatorSheets.CharacterSheet;
            CharacterLevelSheet = simulatorSheets.CharacterLevelSheet;
            EquipmentItemSetEffectSheet = simulatorSheets.EquipmentItemSetEffectSheet;
            Log = new BattleLog();
            player.Simulator = this;
            Player = player;
            Player.Use(foods);
            Player.Stats.EqualizeCurrentHPWithHP();
        }

        public static List<ItemBase> SetReward(
            WeightedSelector<StageSheet.RewardData> itemSelector,
            int maxCount,
            IRandom random,
            MaterialItemSheet materialItemSheet
        )
        {
            var reward = new List<ItemBase>();

            while (reward.Count < maxCount)
            {
                try
                {
                    var data = itemSelector.SelectV2(1).First();
                    if (materialItemSheet.TryGetValue(data.ItemId, out var itemData))
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

            reward = reward.OrderBy(r => r.Id).ToList();
            return reward;
        }

        public static List<ItemBase> SetRewardV2(
            WeightedSelector<StageSheet.RewardData> itemSelector,
            int maxCount,
            IRandom random,
            MaterialItemSheet materialItemSheet
        )
        {
            var reward = new List<ItemBase>();

            while (reward.Count < maxCount)
            {
                try
                {
                    var data = itemSelector.Select(1).First();
                    if (materialItemSheet.TryGetValue(data.ItemId, out var itemData))
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

            reward = reward.OrderBy(r => r.Id).ToList();
            return reward;
        }
    }
}
