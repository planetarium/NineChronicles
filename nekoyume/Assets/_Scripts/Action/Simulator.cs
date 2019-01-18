using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.Model.BattleLog;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Simulator
    {
        public readonly List<LogBase> logs;
        public bool isLose = false;
        private readonly List<CharacterBase> characters;
        private readonly int _seed;
        private readonly int _stage;
        private int time = 0;
        private string result;

        public Simulator(int seed, Model.Avatar avatar)
        {
            //TODO generate random using seed
            _seed = seed;
            _stage = avatar.WorldStage;
            characters = new List<CharacterBase>();
            logs = new List<LogBase>();
            var player = new Player(avatar, this);
            MonsterSpawn(player);
            Add(player);
        }

        public Player Simulate()
        {
            var ss = new StartStage(_stage);
            logs.Add(ss);
            foreach (var character in characters)
            {
                var spawn = new Spawn(character);
                logs.Add(spawn);
                Debug.Log(character);
            }

            var player = (Player) characters.First(c => c is Player);
            while (true)
            {
                foreach (var character in characters)
                {
                    character.Tick();
                }
                time++;
                if (time >= 100)
                {
                    result = "finish";
                    break;
                }

                if (player.targets.Count == 0)
                {
                    player.stage++;

                    result = "win";
                    Debug.Log("win");
                    break;
                }

                if (isLose)
                {
                    result = "lose";
                    Debug.Log("lose");
                    break;
                }
            }
            var log = new BattleResult(result);
            logs.Add(log);
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

            int monsterCount = 2;
            var monsterTable = new Table<Data.Table.Monster>();
            var path2 = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/DataTable/monsters.csv");
            monsterTable.Load(File.ReadAllText(path2));
            for (int i = 0; i < monsterCount; i++)
            {
                MonsterAppear appearData = selector.Select();
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
