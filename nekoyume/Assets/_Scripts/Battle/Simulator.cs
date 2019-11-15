using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class Simulator
    {
        public readonly IRandom Random;
        public readonly int WorldStage;
        private readonly List<Wave> _waves;
        public readonly BattleLog Log;
        public bool Lose = false;
        public readonly Player Player;
        private BattleLog.Result _result;
        public BattleLog.Result Result => _result;
        private int _totalWave;
        public SimplePriorityQueue<CharacterBase> Characters;
        private readonly List<List<ItemBase>> _waveRewards;
        public List<ItemBase> rewards => _waveRewards.SelectMany(i => i).ToList();
        public const float TurnPriority = 100f;
        public CollectionMap ItemMap = new CollectionMap();
        public readonly TableSheets TableSheets;

        public Simulator(IRandom random, AvatarState avatarState, List<Consumable> foods, int worldStage,
            Game.Skill skill = null, TableSheetsState tableSheetsState = null)
        {
            Random = random;
            TableSheets = TableSheets.FromTableSheetsState(tableSheetsState);
            WorldStage = worldStage;
            Log = new BattleLog();
            _waves = new List<Wave>();
            Player = new Player(avatarState, this);
            Player.Use(foods);
            Player.Stats.EqualizeCurrentHPWithHP();
            if (!ReferenceEquals(skill, null))
                Player.OverrideSkill(skill);
            _waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public Player Simulate()
        {
            Log.worldStage = WorldStage;
            Player.Spawn();
            foreach (var wave in _waves)
            {
                Characters = new SimplePriorityQueue<CharacterBase>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);
                int lastWave = _totalWave - 1;
                wave.Spawn(this);
                while (true)
                {
                    if (Characters.TryDequeue(out var character))
                    {
                        character.Tick();
                    }
                    else
                    {
                        break;
                    }


                    if (!Player.Targets.Any())
                    {
                        var index = Math.Min(_waves.IndexOf(wave), lastWave);
                        var items = _waveRewards[index];
                        Player.GetExp(wave.Exp, true);

                        var dropBox = new DropBox(null, items);
                        Log.Add(dropBox);

                        if (index == lastWave)
                        {
                            var stageSheet = TableSheets.StageSheet;
                            if (WorldStage == Player.worldStage
                                && Player.worldStage < stageSheet.Last.Id)
                            {
                                Player.worldStage++;
                            }

                            _result = BattleLog.Result.Win;
                            var rewards = _waveRewards.SelectMany(i => i).ToList();
                            ItemMap = Player.GetRewards(rewards);
                            var getReward = new GetReward(null, rewards);
                            Log.Add(getReward);
                        }

                        break;
                    }

                    if (Lose)
                    {
                        _result = BattleLog.Result.Lose;
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6f;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                if (Lose)
                {
                    break;
                }
            }

            Log.result = _result;
            return Player;
        }

        private void SetWave()
        {
            var stageSheet = TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(WorldStage, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), WorldStage.ToString());

            var waves = stageRow.Waves;
            _totalWave = waves.Count;
            foreach (var waveData in waves)
            {
                var wave = SpawnWave(waveData);
                _waves.Add(wave);
                GetReward(waveData.RewardId);
            }
        }

        private Wave SpawnWave(StageSheet.WaveData waveData)
        {
            var wave = new Wave();
            var monsterTable = TableSheets.CharacterSheet;
            foreach (var monsterData in waveData.Monsters)
            {
                for (int i = 0; i < monsterData.Count; i++)
                {
                    monsterTable.TryGetValue(monsterData.CharacterId, out var row, true);
                    var enemyModel = new Enemy(Player, row, monsterData.Level);

                    wave.Add(enemyModel);
                    wave.IsBoss = waveData.IsBoss;
                }

                wave.Exp = waveData.Exp;
            }

            return wave;
        }

        private void GetReward(int id)
        {
            var rewardTable = TableSheets.StageRewardSheet;
            var itemSelector = new WeightedSelector<int>(Random);
            var items = new List<ItemBase>();
            if (rewardTable.TryGetValue(id, out var reward))
            {
                foreach (var r in reward.Rewards)
                {
                    if (r.Ratio <= 0m)
                    {
                        continue;
                    }

                    itemSelector.Add(r.ItemId, r.Ratio);
                    try
                    {
                        var itemId = itemSelector.Pop();
                        if (TableSheets.MaterialItemSheet.TryGetValue(itemId, out var itemData))
                        {
                            var count = Random.Next(r.Min, r.Max + 1);
                            for (var i = 0; i < count; i++)
                            {
                                var guid = Random.GenerateRandomGuid();
                                var item = ItemFactory.Create(itemData, guid);
                                items.Add(item);
                            }
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            _waveRewards.Add(items);
        }
    }
}
