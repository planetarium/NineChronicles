using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class StageSimulator : Simulator
    {
        private readonly int _worldId;
        public readonly int StageId;
        private readonly List<Wave> _waves;
        private int _totalWave;
        private readonly List<List<ItemBase>> _waveRewards;
        public IEnumerable<ItemBase> Rewards => _waveRewards.SelectMany(i => i).ToList();
        public CollectionMap ItemMap = new CollectionMap();

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            int worldId,
            int stageId,
            TableSheets tableSheets) : base(random, avatarState, foods, tableSheets)
        {
            _worldId = worldId;
            StageId = stageId;
            _waves = new List<Wave>();
            _waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            int worldId,
            int stageId,
            Model.Skill.Skill skill = null) : base(random, avatarState, foods)
        {
            _worldId = worldId;
            StageId = stageId;
            _waves = new List<Wave>();
            if (!ReferenceEquals(skill, null))
                Player.OverrideSkill(skill);
            _waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public override Player Simulate()
        {
            Log.worldId = _worldId;
            Log.stageId = StageId;
            Player.Spawn();
            var turn = 0;
            foreach (var wave in _waves)
            {
                WaveTurn = 0;
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);
                var lastWave = _totalWave - 1;
                wave.Spawn(this);
                while (true)
                {
                    turn++;
                    if (turn >= MaxTurn)
                    {
                        Result = BattleLog.Result.TimeOver;
                        Lose = true;
                        break;
                    }

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
                            Result = BattleLog.Result.Win;
                            var rewards = _waveRewards.SelectMany(i => i).ToList();
                            ItemMap = Player.GetRewards(rewards);
                            var getReward = new GetReward(null, rewards);
                            Log.Add(getReward);
                        }

                        break;
                    }

                    if (Lose)
                    {
                        Result = BattleLog.Result.Lose;
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                if (Lose)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        private void SetWave()
        {
            var stageSheet = TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId.ToString());

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
                for (var i = 0; i < monsterData.Count; i++)
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
                var rewards = reward.Rewards.Where(r => r.Ratio > 0m);
                foreach (var r in rewards)
                {
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
