using System.Collections.Generic;
using Nekoyume.Action;
using UnityEngine;

namespace Nekoyume.Data.Table
{
    public class Recipe : Row
    {
        private struct Material
        {
            public int materialId;
            public int count;
            public Material(int materialId, int count)
            {
                this.materialId = materialId;
                this.count = count;
            }
        }

        public int Id;
        public int ResultId;
        public int Material1;
        public int Material2;
        public int Material3;
        public int Material4;
        public int Material5;

        public bool IsMatchForConsumable(List<Combination.MaterialRow> materials)
        {
            var recipe = GetRecipe();

            var mCount = materials.Count;
            if (mCount != recipe.Count)
            {
                return false;
            }

            for (var i = 0; i < mCount; i++)
            {
                var m = materials[i];
                int idx = recipe.FindIndex(item => item.materialId == m.row.Id && item.count <= m.count);
                if (idx != -1)
                {
                    recipe.RemoveAt(idx);
                }
                else
                {
                    return false;
                }
            }
            
            return recipe.Count == 0;
        }

        public int GetCombinationResultCountForConsumable(List<Combination.MaterialRow> materials)
        {
            var recipe = GetRecipe();
            var result = 0;

            var mCount = materials.Count;
            for (var i = 0; i < mCount; i++)
            {
                var m = materials[i];
                if (recipe.Exists(item => item.materialId == m.row.Id))
                {
                    var count = m.count / recipe[i].count;
                    result = i == 0 ? count : Mathf.Min(result, count);
                }
                else
                {
                    return 0;
                }
            }
            
            return result;
        }

        private List<Material> GetRecipe()
        {
            var list = new List<Material>();

            if (Material1 != 0)
            {
                list.Add(new Material(Material1, 1));
            }
            if (Material2 != 0)
            {
                list.Add(new Material(Material2, 1));
            }
            if (Material3 != 0)
            {
                list.Add(new Material(Material3, 1));
            }
            if (Material4 != 0)
            {
                list.Add(new Material(Material4, 1));
            }
            if (Material5 != 0)
            {
                list.Add(new Material(Material5, 1));
            }

            return list;
        }
    }
}
