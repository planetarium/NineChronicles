using System.Collections.Generic;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_ItemViewData", menuName = "Scriptable Object/Item View Data",
        order = int.MaxValue)]
    public class ItemViewDataScriptableObject : ScriptableObject
    {
        public List<ItemViewData> datas;
    }
}
