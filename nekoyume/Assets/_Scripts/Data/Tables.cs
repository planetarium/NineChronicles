using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Data
{
    public class Tables : MonoSingleton<Tables>
    {
        public Table<Recipe> Recipe { get; private set; }
        public Table<SetEffect> SetEffect { get; private set; }
        public Table<SkillEffect> SkillEffect { get; private set; }
        public Table<StageDialog> StageDialogs { get; private set; }

        public void Initialize()
        {
            Recipe = new Table<Recipe>();
            Load(Recipe, "DataTable/recipe");

            SetEffect = new Table<SetEffect>();
            Load(SetEffect, "DataTable/set_effect");

            SkillEffect = new Table<SkillEffect>();
            Load(SkillEffect, "DataTable/skill_effect");

            StageDialogs = new Table<StageDialog>();
            Load(StageDialogs, "DataTable/stage_dialog");
        }

        private void Load(ITable table, string filename)
        {
            var file = Resources.Load<TextAsset>(filename);
            if (file != null)
            {
                table.Load(file.text);
            }
        }

        public IEnumerable<IStatMap> GetSetEffect(int id, int count)
        {
            var statMaps = new List<IStatMap>();
            foreach (var row in SetEffect)
            {
                if (row.Value.setId == id)
                {
                    statMaps.Add(row.Value.ToSetEffectMap());
                }
            }

            return statMaps.Take(count).ToArray();
        }
    }
}
