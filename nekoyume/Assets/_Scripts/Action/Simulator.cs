using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Simulator
    {
        private readonly int _seed;
        private readonly int _stage;
        private readonly List<CharacterBase> characters;
        public readonly BattleLog log;
        public bool isLose = false;
        public BattleResult.Result result;

        public Simulator(int seed, Model.Avatar avatar)
        {
            //TODO generate random using seed
            _seed = seed;
            _stage = avatar.WorldStage;
            characters = new List<CharacterBase>();
            log = new BattleLog();
            var player = new Player(avatar, this);
            MonsterSpawn(player);
            Add(player);
        }

        public Player Simulate()
        {
            var ss = new StartStage
            {
                stage = _stage,
            };
            log.Add(ss);
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

                    result = BattleResult.Result.Win;
                    Debug.Log("win");
                    break;
                }

                if (isLose)
                {
                    result = BattleResult.Result.Lose;
                    Debug.Log("lose");
                    break;
                }
            }

            var br = new BattleResult
            {
                result = result
            };
            log.Add(br);
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
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/DataTable/monster_appear.csv");
            appear.Load(File.ReadAllText(path));
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
            var path2 = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/DataTable/monsters.csv");
            monsterTable.Load(File.ReadAllText(path2));
            for (var i = 0; i < monsterCount; i++)
            {
                var appearData = selector.Select();
                Data.Table.Monster monsterData;
                if (monsterTable.TryGetValue(appearData.MonsterId, out monsterData))
                {
                    var monster = new Model.Monster(monsterData, player);
                    player.targets.Add(monster);
                    Add(monster);
                }
            }
        }
    }
}
