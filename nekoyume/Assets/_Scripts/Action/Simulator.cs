using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Simulator
    {
        public readonly IRandom Random;
        private readonly int _stage;
        private readonly List<MonsterWave> _waves;
        public readonly BattleLog Log;
        public bool Lose = false;
        public readonly Player Player;
        private BattleLog.Result _result;
        private int _totalWave;
        public List<CharacterBase> Characters;
        private readonly List<List<ItemBase>> _waveRewards;
        
        public Simulator(IRandom random, Model.Avatar avatar, List<Food> foods, int stage)
        {
            Random = random;
            _stage = stage;
            Log = new BattleLog();
            _waves = new List<MonsterWave>();
            Player = new Player(avatar, this);
            Player.Use(foods);
            _waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public Player Simulate()
        {
            Log.stage = _stage;
            Player.Spawn();
            foreach (var wave in _waves)
            {
                Characters = new List<CharacterBase> {Player};
                int lastWave = _totalWave - 1;
                wave.Spawn(this);
                while (true)
                {
                    var characters = Characters.ToList();
                    foreach (var character in characters) character.Tick();

                    if (Player.targets.Count == 0)
                    {
                        var index = Math.Min(_waves.IndexOf(wave), lastWave);
                        var items = _waveRewards[index];
                        Player.GetExp(wave.exp, true);

                        var dropBox = new DropBox
                        {
                            items = items
                        };
                        Log.Add(dropBox);

                        if (index == lastWave)
                        {
                            Player.stage++;
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
                if (row.Value.stage == _stage)
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
                    Character characterData;
                    if (!monsterTable.TryGetValue(monsterData.id, out characterData))
                    {
                        Debug.Log(monsterData.id);
                    }
                    wave.Add(new Monster(characterData, monsterData.level, Player));
                    wave.isBoss = stage.isBoss;
                }

                wave.exp = stage.exp;
            }

            return wave;
        }

        private void GetReward(int id)
        {
            var rewardTable = Tables.instance.StageReward;
            var itemTable = Tables.instance.Item;
            var itemSelector = new WeightedSelector<int>(Random);
            StageReward reward;
            var items = new List<ItemBase>();
            if (rewardTable.TryGetValue(id, out reward))
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
                    Item itemData;
                    if (itemTable.TryGetValue(itemId, out itemData))
                    {
                        var count = Random.Next(r.range[0], r.range[1]);
                        for (int i = 0; i < count; i++)
                        {
                            var item = ItemBase.ItemFactory(itemData);
                            items.Add(item);
                        }
                    }
                }
            }
            _waveRewards.Add(items);
        }
    }
}
