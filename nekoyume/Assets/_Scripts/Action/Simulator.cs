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
        private readonly IRandom _seed;
        private readonly int _stage;
        private readonly List<List<Model.Monster>> waves;
        public readonly BattleLog Log;
        public bool Lose = false;
        private readonly Player _player;
        private BattleLog.Result _result;
        private int _totalWave;
        public List<CharacterBase> Characters;

        public Simulator(IRandom seed, Model.Avatar avatar)
        {
            //TODO generate random using seed
            _seed = seed;
            _stage = avatar.WorldStage;
            Log = new BattleLog();
            waves = new List<List<Model.Monster>>();
            _player = new Player(avatar, this);
            SetWave();
        }

        public Player Simulate()
        {
            Log.stage = _stage;
            _player.Spawn();
            foreach (var wave in waves)
            {
                Characters = new List<CharacterBase> {_player};
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
                        if (waves.IndexOf(wave) == _totalWave - 1)
                        {
                            _player.stage++;
                            _result = BattleLog.Result.Win;
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
                foreach (var waveData in stageData.Monsters)
                {
                    var monsterId = waveData.Key;
                    var count = waveData.Value;
                    if (count <= 0) continue;
                    for (var i = 0; i < count; i++)
                    {
                        var monster = SpawnMonster(monsterId);
                        monsters.Add(monster);
                    }
                }
                waves.Add(monsters);
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
            var dropTable = tables.ItemDrop;
            var itemTable = tables.Item;
            var itemSelector = new WeightedSelector<int>(_seed);

            Data.Table.Monster monsterData;
            if (!monsterTable.TryGetValue(monsterId, out monsterData))
            {
                Debug.Log(monsterId);
            }
            foreach (var pair in dropTable)
            {
                ItemDrop dropData = pair.Value;
                if (monsterData.Id != dropData.MonsterId)
                    continue;

                if (dropData.Weight <= 0)
                    continue;

                itemSelector.Add(dropData.ItemId, dropData.Weight);
            }

            var itemId = itemSelector.Select();
            ItemBase item = null;
            Item itemData;
            if (itemSelector.Count > 0 && itemTable.TryGetValue(itemId, out itemData))
            {
                item = ItemBase.ItemFactory(itemData);
            }
            return new Model.Monster(monsterData, _player, item);
        }
    }
}
