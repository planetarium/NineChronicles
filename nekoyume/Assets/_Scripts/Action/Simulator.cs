using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Simulator
    {
        private const string KAppearPath = "Assets/Resources/DataTable/monster_appear.csv";
        private const string KMonstersPath = "Assets/Resources/DataTable/monsters.csv";
        private const string KItemDropPath = "Assets/Resources/DataTable/item_drop.csv";
        internal const string StatsPath = "Assets/Resources/DataTable/stats.csv";
        private readonly int _seed;
        private readonly int _stage;
        private readonly List<CharacterBase> characters;
        public readonly BattleLog Log;
        public bool isLose = false;
        private BattleLog.Result _result;

        public Simulator(int seed, Model.Avatar avatar)
        {
            //TODO generate random using seed
            _seed = seed;
            _stage = avatar.WorldStage;
            characters = new List<CharacterBase>();
            Log = new BattleLog();
            var player = new Player(avatar, this);
            MonsterSpawn(player);
            Add(player);
        }

        public Player Simulate()
        {
            Log.stage = _stage;
            foreach (var character in characters)
            {
                character.Spawn();
                Debug.Log(character);
            }

            var player = (Player) characters.First(c => c is Player);
            while (true)
            {
                foreach (var character in characters) character.Tick();

                if (player.targets.Count == 0)
                {
                    player.stage++;

                    _result = BattleLog.Result.Win;
                    Debug.Log("win");
                    break;
                }

                if (isLose)
                {
                    _result = BattleLog.Result.Lose;
                    Debug.Log("lose");
                    break;
                }
            }

            Log.result = _result;
            return (Player) characters.First(c => c is Player);
        }

        private void Add(CharacterBase character)
        {
            character.InitAI();
            characters.Add(character);
        }

        private void MonsterSpawn(Player player)
        {
            var selector = new WeightedSelector<MonsterAppear>();
            var appear = new Table<MonsterAppear>();
            var appearPath = Path.Combine(Directory.GetCurrentDirectory(), KAppearPath);
            appear.Load(File.ReadAllText(appearPath));
            foreach (var pair in appear)
            {
                var data = pair.Value;
                if (_stage > data.StageMax)
                    continue;

                if (data.Weight <= 0)
                    continue;

                selector.Add(data, data.Weight);
            }

            var monsterCount = 2;
            var monsterTable = new Table<Data.Table.Monster>();
            var monsterPath = Path.Combine(Directory.GetCurrentDirectory(), KMonstersPath);
            monsterTable.Load(File.ReadAllText(monsterPath));

            var itemTable = Agent.ItemTable();
            var itemSelector = new WeightedSelector<int>();
            var dropTable = new Table<ItemDrop>();
            dropTable.Load(File.ReadAllText(KItemDropPath));

            for (var i = 0; i < monsterCount; i++)
            {
                var appearData = selector.Select();
                Data.Table.Monster monsterData;
                if (monsterTable.TryGetValue(appearData.MonsterId, out monsterData))
                {
                    foreach (var pair in dropTable)
                    {
                        ItemDrop dropData = pair.Value;
                        if (monsterData.Id != dropData.MonsterId)
                            continue;

                        if (dropData.Weight <= 0)
                            continue;

                        itemSelector.Add(dropData.ItemId, dropData.Weight);
                    }

                    int itemId = itemSelector.Select();
                    ItemBase item = null;
                    Item itemData;
                    if (itemSelector.Count > 0 && itemTable.TryGetValue(itemId, out itemData))
                    {
                        item = ItemBase.ItemFactory(itemData);
                    }
                    var monster = new Model.Monster(monsterData, player, item);
                    player.targets.Add(monster);
                    Add(monster);
                }
            }
        }
    }
}
