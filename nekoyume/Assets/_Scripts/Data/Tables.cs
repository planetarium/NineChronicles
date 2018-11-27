using System.Linq;
using UnityEngine;
using Nekoyume.Data.Table;


namespace Nekoyume.Data
{
    public class Tables : MonoBehaviour
    {
        public Table<Stats> Stats { get; private set; }
        public Table<Skill> Skill { get; private set; }
        public Table<Stage> Stage { get; private set; }
        public Table<MonsterAppear> MonsterAppear { get; private set; }
        public Table<Monster> Monster { get; private set; }

        private void Start()
        {
            Stats = new Table<Stats>();
            Load(Stats, "DataTable/stats");

            Skill = new Table<Skill>();
            Load(Skill, "DataTable/skills");
            Load(Skill, "DataTable/monster_skills");

            Stage = new Table<Stage>();
            Load(Stage, "DataTable/stage");

            MonsterAppear = new Table<MonsterAppear>();
            Load(MonsterAppear, "DataTable/monster_appear");

            Monster = new Table<Monster>();
            Load(Monster, "DataTable/monsters");
        }

        private void Load(ITable table, string filename)
        {
            TextAsset file = Resources.Load<TextAsset>(filename);
            if (file != null)
            {
                table.Load(file.text);
            }
        }

        public int GetLevel(int exp)
        {
            var q = Stats.Select(row => row.Value);
            var enumerable = q as Stats[] ?? q.ToArray();
            Stats data = enumerable.LastOrDefault(row => row.Exp <= exp) ?? Stats[1];
            return data.Id;
        }
    }
}
