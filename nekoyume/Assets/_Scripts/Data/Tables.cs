using UnityEngine;
using Nekoyume.Data.Table;


namespace Nekoyume.Data
{
    public class Tables : MonoBehaviour
    {
        Table<Stats> Stats { get; set; }
        Table<Skill> Skill { get; set; }
        Table<Stage> Stage { get; set; }
        Table<MonsterAppear> MonsterAppear { get; set; }
        Table<Monster> Monster { get; set; }

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
    }
}
