using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
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
        private readonly HashSet<int> _dropIds;
        private readonly List<List<ItemBase>> _waveRewards;
        private const int DropCapacity = 3;

        public Simulator(IRandom random, Model.Avatar avatar)
        {
            Random = random;
            _stage = avatar.WorldStage;
            Log = new BattleLog();
            _waves = new List<MonsterWave>();
            Player = new Player(avatar, this);
            _dropIds = new HashSet<int>();
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
            var tables = ActionManager.Instance.tables;
            var waveTable = tables.MonsterWave;
            var stageData = waveTable[_stage];
            _totalWave = stageData.Wave;
            for (var w = 0; w < _totalWave; w++)
            {
                var wave = new MonsterWave();
                var items = new List<ItemBase>();
                foreach (var waveData in stageData.Monsters)
                {
                    var monsterId = waveData.Key;
                    var count = waveData.Value;
                    if (count <= 0) continue;
                    for (var i = 0; i < count; i++)
                    {
                        var monster = SpawnMonster(monsterId);
                        wave.Add(monster);
                        var item = GetItem(monster.data.id);
                        if (!ReferenceEquals(item, null))
                        {
                            items.Add(item);
                        }
                    }
                }
                _waves.Add(wave);
                _waveRewards.Add(items);
            }

            if (stageData.BossId > 0)
            {
                var boss = SpawnMonster(stageData.BossId);
                var bossWave = new MonsterWave();
                bossWave.Add(boss);
                _waves.Add(bossWave);
            }
        }

        private Monster SpawnMonster(int monsterId)
        {
            var tables = ActionManager.Instance.tables;
            var monsterTable = tables.Character;

            Character characterData;
            if (!monsterTable.TryGetValue(monsterId, out characterData))
            {
                Debug.Log(monsterId);
            }
            return new Monster(characterData, Player);
        }

        private ItemBase GetItem(int monsterId)
        {
            var tables = ActionManager.Instance.tables;
            var dropTable = tables.ItemDrop;
            var itemTable = tables.Item;
            var itemSelector = new WeightedSelector<int>(Random);
            foreach (var pair in dropTable)
            {
                ItemDrop dropData = pair.Value;
                if (monsterId != dropData.MonsterId)
                    continue;

                if (dropData.Weight <= 0)
                    continue;

                itemSelector.Add(dropData.ItemId, dropData.Weight);
            }

            var itemId = itemSelector.Select();
            Item itemData;
            if (itemSelector.Count > 0 && itemTable.TryGetValue(itemId, out itemData))
            {
                if (_dropIds.Count < DropCapacity)
                {
                    _dropIds.Add(itemId);
                }

                if (_dropIds.Contains(itemId))
                {
                    var item = ItemBase.ItemFactory(itemData);
                    return item;
                }
            }

            return null;
        }
    }
}
