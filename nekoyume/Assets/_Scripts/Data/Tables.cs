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
        public Table<SkillEffect> SkillEffect { get; private set; }
        public Table<StageDialog> StageDialogs { get; private set; }

        public void Initialize()
        {
            Recipe = new Table<Recipe>();
            Load(Recipe, "DataTable/recipe");

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
    }
}
