using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "SpineTooltipData", menuName = "Scriptable Object/Spine Tooltip Data",
        order = int.MaxValue)]
    public class SpineTooltipDataScriptableObject : ScriptableObject
    {
        [SerializeField]
        private List<SpineTooltipData> datas;

        public List<SpineTooltipData> Datas => datas;

        public SpineTooltipData GetSpineTooltipData(int id)
        {
            SpineTooltipData data = null;
            data = datas.FirstOrDefault(x => x.ResourceID == id);
            return data;
        }
    }
}
