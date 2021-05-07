using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_ItemViewData", menuName = "Scriptable Object/Item View Data",
        order = int.MaxValue)]
    public class ItemViewDataScriptableObject : ScriptableObject
    {
        [SerializeField]
        private int fallbackGrade;

        [SerializeField]
        private List<ItemViewData> datas;

        public ItemViewData GetItemViewData(int grade)
        {
            ItemViewData data = null;
            data = datas.FirstOrDefault(x => x.Grade == grade);
            if (data is null)
            {
                data = datas.FirstOrDefault(x => x.Grade == fallbackGrade);
            }

            return data;
        }
    }
}
