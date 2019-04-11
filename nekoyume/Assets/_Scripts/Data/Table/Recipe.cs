using System.Collections.Generic;
using Nekoyume.Action;
using UnityEngine;

namespace Nekoyume.Data.Table
{
    public class Recipe : Row
    {
        public int Id;
        public int Material1;
        public int Material2;
        public int Material3;
        public int Material4;
        public int Material5;

        public bool IsMatch(List<Combination.ItemModel> materials)
        {
            var dic = GetRecipe();

            var mCount = materials.Count;
            if (mCount != dic.Count)
            {
                return false;
            }
            
            for (var i = 0; i < mCount; i++)
            {
                var m = materials[i];
                if (dic.ContainsKey(m.id) &&
                    dic[m.id] <= m.count)
                {
                    dic.Remove(m.id);
                }
                else
                {
                    return false;
                }
            }

            return dic.Count == 0;
        }

        public int CalculateCount(List<Combination.ItemModel> materials)
        {

            var dic = GetRecipe();
            var result = 0;

            var mCount = materials.Count;
            for (var i = 0; i < mCount; i++)
            {
                var m = materials[i];
                if (dic.ContainsKey(m.id))
                {
                    var count = Mathf.FloorToInt((float)m.count / dic[m.id]);
                    result = i == 0 ? count : Mathf.Min(result, count);
                }
                else
                {
                    return 0;
                }
            }
            
            return result;
        }

        private Dictionary<int, int> GetRecipe()
        {
            var dic = new Dictionary<int, int>();
            if (Material1 != 0)
            {
                dic.Add(Material1, 1);
            }
            if (Material2 != 0)
            {
                if (dic.ContainsKey(Material2))
                {
                    dic[Material2] += 1;
                }
                else
                {
                    dic.Add(Material2, 1);   
                }
            }
            if (Material3 != 0)
            {
                if (dic.ContainsKey(Material3))
                {
                    dic[Material3] += 1;
                }
                else
                {
                    dic.Add(Material3, 1);
                }
            }
            if (Material4 != 0)
            {
                if (dic.ContainsKey(Material4))
                {
                    dic[Material4] += 1;
                }
                else
                {
                    dic.Add(Material4, 1);
                }
            }
            if (Material5 != 0)
            {
                if (dic.ContainsKey(Material5))
                {
                    dic[Material5] += 1;
                }
                else
                {
                    dic.Add(Material5, 1);
                }
            }

            return dic;
        }
    }
}
