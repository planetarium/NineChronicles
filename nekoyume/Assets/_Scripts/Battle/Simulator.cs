using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using Priority_Queue;
using UnityEngine;

namespace Nekoyume.Battle
{
    public class Simulator
    {
        private readonly IActionContext _ctx;
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

        public Simulator(IActionContext ctx, AvatarState avatarState, List<Food> foods, int worldStage,
            Game.Skill skill = null)
        {
            _ctx = ctx;
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
                            var stages = Game.Game.instance.TableSheets.StageSheet.ToOrderedList();
                            if (_worldStage == Player.worldStage
                                && Player.worldStage < stages.Last().Stage)
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
            var stageTable = Game.Game.instance.TableSheets.StageSheet.ToOrderedList();
            var waves = stageTable.Where(row => row.Stage == _worldStage).ToList();
            _totalWave = waves.Count;
            foreach (var w in waves)
            {
                var wave = SpawnWave(w);
                _waves.Add(wave);
                GetReward(w.Reward);
            }
        }

        private MonsterWave SpawnWave(StageSheet.Row stage)
        {
            var wave = new MonsterWave();
            var monsterTable = Game.Game.instance.TableSheets.CharacterSheet;
            foreach (var monsterData in stage.Monsters)
            {
                for (int i = 0; i < monsterData.Count; i++)
                {
                    if (!monsterTable.TryGetValue(monsterData.CharacterId, out var characterRow))
                    {
                        Debug.Log(monsterData.CharacterId);
                    }

                    wave.Add(new Monster(characterRow, monsterData.Level, Player));
                    wave.IsBoss = stage.IsBoss;
                }

                wave.EXP = stage.Exp;
            }

            return wave;
        }

        private void GetReward(int id)
        {
            IRandom random = _ctx.Random;
            var rewardTable = Game.Game.instance.TableSheets.StageRewardSheet;
            var itemTable = Tables.instance.Item;
            var itemSelector = new WeightedSelector<int>(random);
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
                    var itemId = itemSelector.Pop();
                    if (itemTable.TryGetValue(itemId, out var itemData))
                    {
                        var count = _ctx.Random.Next(r.Min, r.Max);
                        for (int i = 0; i < count; i++)
                        {
                            var guid = _ctx.NewGuid();
                            var item = ItemFactory.Create(itemData, guid);
                            items.Add(item);
                        }
                    }
                }
            }

            _waveRewards.Add(items);
        }
    }
}
