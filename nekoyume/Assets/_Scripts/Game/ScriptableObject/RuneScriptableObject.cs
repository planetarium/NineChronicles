using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_RuneData", menuName = "Scriptable Object/Rune Data",
        order = int.MaxValue)]
    public class RuneScriptableObject : ScriptableObject
    {
        public List<RuneData> Runes;
        public Sprite DefaultRuneIcon;

        [Serializable]
        public class RuneData
        {
            public int id;
            public string ticker;
            public Sprite icon;
            public int sortingOrder;
        }
    }
}
