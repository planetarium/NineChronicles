using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Model.Item;
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

        public ItemViewData GetItemViewData(ItemBase itemBase)
        {
            var upgrade = 0;
            // if itemBase is TradableMaterial, upgrade view data.
            if (itemBase.ItemSubType != ItemSubType.Circle &&
                itemBase.ItemSubType != ItemSubType.Scroll &&
                itemBase is TradableMaterial)
            {
                upgrade = 1;
            }

            // it can be overflown if the item is already at the highest grade.
            return GetItemViewData(itemBase.Grade + upgrade);
        }
    }
}
