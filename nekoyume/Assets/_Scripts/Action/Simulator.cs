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
        private readonly List<List<Model.Monster>> waves;
        public readonly BattleLog Log;
        public bool Lose = false;
        private readonly Player _player;
        private BattleLog.Result _result;
        private int _totalWave;
        public List<CharacterBase> Characters;
        private readonly HashSet<int> _dropIds;
        private readonly List<List<ItemBase>> waveRewards;
        private const int DropCapacity = 3;

        public Simulator(IRandom random, Model.Avatar avatar)
        {
            Random = random;
            _stage = avatar.WorldStage;
            Log = new BattleLog();
            waves = new List<List<Model.Monster>>();
            _player = new Player(avatar, this);
            _dropIds = new HashSet<int>();
            waveRewards = new List<List<ItemBase>>();
            SetWave();
        }

        public Player Simulate()
        {
            Log.stage = _stage;
            _player.Spawn();
            foreach (var wave in waves)
            {
                Characters = new List<CharacterBase> {_player};
                int lastWave = _totalWave - 1;
                foreach (var monster in wave)
                {
                    _player.targets.Add(monster);
                    Characters.Add(monster);
                    monster.Spawn();
                }
                while (true)
                {
                    var characters = Characters.ToList();
                    foreach (var character in characters) character.Tick();

                    if (_player.targets.Count == 0)
                    {
                        var index = Math.Min(waves.IndexOf(wave), lastWave);
                        var items = waveRewards[index];
                        var dropBox = new DropBox
                        {
                            items = items
                        };
                        Log.Add(dropBox);

                        if (index == lastWave)
                        {
                            _player.stage++;
                            _result = BattleLog.Result.Win;
                            var getReward = new GetReward
                            {
                                rewards = waveRewards.SelectMany(i => i).ToList()
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
            return _player;
        }

        private void SetWave()
        {
            var tables = ActionManager.Instance.tables;
            var waveTable = tables.MonsterWave;
            var stageData = waveTable[_stage];
            _totalWave = stageData.Wave;
            for (var wave = 0; wave < _totalWave; wave++)
            {
                var monsters = new List<Model.Monster>();
                var items = new List<ItemBase>();
                foreach (var waveData in stageData.Monsters)
                {
                    var monsterId = waveData.Key;
                    var count = waveData.Value;
                    if (count <= 0) continue;
                    for (var i = 0; i < count; i++)
                    {
                        var monster = SpawnMonster(monsterId);
                        monsters.Add(monster);
                        var item = GetItem(monster.data.Id);
                        if (!ReferenceEquals(item, null))
                        {
                            items.Add(item);
                        }
                    }
                }
                waves.Add(monsters);
                waveRewards.Add(items);
            }

            if (stageData.BossId > 0)
            {
                var boss = SpawnMonster(stageData.BossId);
                waves.Add(new List<Model.Monster>{boss});
            }
        }

        private Model.Monster SpawnMonster(int monsterId)
        {
            var tables = ActionManager.Instance.tables;
            var monsterTable = tables.Monster;

            Data.Table.Monster monsterData;
            if (!monsterTable.TryGetValue(monsterId, out monsterData))
            {
                Debug.Log(monsterId);
            }
            return new Model.Monster(monsterData, _player);
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
