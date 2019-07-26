using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.State;
using Priority_Queue;
using UnityEngine;

namespace Nekoyume.Battle
{
    public class Simulator
    {
        public readonly IRandom Random;
        private readonly int _worldStage;
        private readonly List<MonsterWave> _waves;
        public readonly BattleLog Log;
        public bool Lose = false;
        public readonly Player Player;
        private BattleLog.Result _result;
        private int _totalWave;
        public SimplePriorityQueue<CharacterBase> Characters;
        private readonly List<List<ItemBase>> _waveRewards;
        public List<ItemBase> rewards => _waveRewards.SelectMany(i => i).ToList();
        public const float TurnPriority = 100f;

        public Simulator(IRandom random, AvatarState avatarState, List<Food> foods, int worldStage, SkillBase skill=null)
        {
            Random = random;
            _worldStage = worldStage;
            Log = new BattleLog();
            _waves = new List<MonsterWave>();
            Player = new Player(avatarState, this);
            Player.Use(foods);
            if (!ReferenceEquals(skill, null))
                Player.OverrideSkill(skill);
            _waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public Player Simulate()
        {
            Log.worldStage = _worldStage;
            Player.Spawn();
            foreach (var wave in _waves)
            {
                Characters = new SimplePriorityQueue<CharacterBase>();
                Characters.Enqueue(Player, TurnPriority / Player.TurnSpeed);
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


                    if (!Player.targets.Any())
                    {
                        var index = Math.Min(_waves.IndexOf(wave), lastWave);
                        var items = _waveRewards[index];
                        Player.GetExp(wave.EXP, true);

                        var dropBox = new DropBox
                        {
                            items = items
                        };
                        Log.Add(dropBox);

                        if (index == lastWave)
                        {
                            if (_worldStage == Player.worldStage)
                            {
                                Player.worldStage++;
                            }

                            _result = BattleLog.Result.Win;
                            var rewards = _waveRewards.SelectMany(i => i).ToList();
                            Player.GetRewards(rewards);
                            var getReward = new GetReward
                            {
                                rewards = rewards,
                            };
                            Log.Add(getReward);
                            Debug.Log("win");
                        }
                        break;
                    }

                    if (Lose)
                    {
                        _result = BattleLog.Result.Lose;
                        Debug.Log("lose");
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6f;
                        Characters.UpdatePriority(other, speed);
                    }
                    Characters.Enqueue(character, TurnPriority / character.TurnSpeed);
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
            var stageTable = Tables.instance.Stage;
            var waves = new List<Stage>();
            foreach (var row in stageTable)
            {
                if (row.Value.stage == _worldStage)
                {
                    waves.Add(row.Value);
                }

            }
            _totalWave = waves.Count;
            foreach (var w in waves)
            {
                var wave = SpawnWave(w);
                _waves.Add(wave);
                GetReward(w.reward);
            }
        }

        private MonsterWave SpawnWave(Stage stage)
        {
            var wave = new MonsterWave();
            var monsterTable = Tables.instance.Character;
            foreach (var monsterData in stage.Monsters())
            {
                for (int i = 0; i < monsterData.count; i++)
                {
                    if (!monsterTable.TryGetValue(monsterData.id, out var characterData))
                    {
                        Debug.Log(monsterData.id);
                    }
                    wave.Add(new Monster(characterData, monsterData.level, Player));
                    wave.IsBoss = stage.isBoss;
                }

                wave.EXP = stage.exp;
            }

            return wave;
        }

        private void GetReward(int id)
        {
            var rewardTable = Tables.instance.StageReward;
            var itemTable = Tables.instance.Item;
            var itemSelector = new WeightedSelector<int>(Random);
            var items = new List<ItemBase>();
            if (rewardTable.TryGetValue(id, out var reward))
            {
                var rewards = reward.Rewards();
                foreach (var r in rewards)
                {
                    if (r.ratio <= 0f)
                    {
                        continue;
                    }
                    itemSelector.Add(r.id, r.ratio);
                    var itemId = itemSelector.Pop();
                    if (itemTable.TryGetValue(itemId, out var itemData))
                    {
                        var count = Random.Next(r.range[0], r.range[1]);
                        for (int i = 0; i < count; i++)
                        {
                            var b = new byte[16];
                            Random.NextBytes(b);
                            var guid = new Guid(b);
                            var item = ItemBase.ItemFactory(itemData, guid);
                            items.Add(item);
                        }
                    }
                }
            }
            _waveRewards.Add(items);
        }
    }
}
