using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "CostIconData", menuName = "Scriptable Object/Cost Icon Data",
           order = int.MaxValue)]
    public class CostIconDataScriptableObject : ScriptableObject
    {
        [Serializable]
        private struct CostIconData
        {
            public CostType CostType;
            public Sprite Sprite;
        }

        [SerializeField]
        private List<CostIconData> _costIcons = null;

        public Sprite GetIcon(CostType type)
        {
            return _costIcons.Find(x => x.CostType == type).Sprite;
        }
    }
}
