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

        public Simulator(IRandom random, AvatarState avatarState, List<Consumable> foods, int worldStage,
            Game.Skill skill = null)
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
                            var stageSheet = Game.Game.instance.TableSheets.StageSheet;
                            if (_worldStage == Player.worldStage
                                && Player.worldStage < stageSheet.Last.Id)
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
            var stageSheet = Game.Game.instance.TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(_worldStage, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), _worldStage.ToString());

            var waves = stageRow.Waves;
            _totalWave = waves.Count;
            foreach (var waveData in waves)
            {
                var wave = SpawnWave(waveData);
                _waves.Add(wave);
                GetReward(waveData.RewardId);
            }
        }

        private MonsterWave SpawnWave(StageSheet.WaveData waveData)
        {
            var wave = new MonsterWave();
            var monsterTable = Game.Game.instance.TableSheets.CharacterSheet;
            foreach (var monsterData in waveData.Monsters)
            {
                for (int i = 0; i < monsterData.Count; i++)
                {
                    if (!monsterTable.TryGetValue(monsterData.CharacterId, out var characterRow))
                    {
                        Debug.Log(monsterData.CharacterId);
                    }

                    wave.Add(new Monster(characterRow, monsterData.Level, Player));
                    wave.IsBoss = waveData.IsBoss;
                }

                wave.EXP = waveData.Exp;
            }

            return wave;
        }

        private void GetReward(int id)
        {
            var rewardTable = Game.Game.instance.TableSheets.StageRewardSheet;
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
                    var itemId = itemSelector.Pop();
                    if (Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(itemId, out var itemData))
                    {
                        var count = Random.Next(r.Min, r.Max);
                        for (int i = 0; i < count; i++)
                        {
                            var guid = Random.GenerateRandomGuid();
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
