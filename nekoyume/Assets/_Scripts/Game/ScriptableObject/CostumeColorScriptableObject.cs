using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(
        fileName = "UI_CostumeColor",
        menuName = "Scriptable Object/Costume Color",
        order = int.MaxValue)]
    public class CostumeColorScriptableObject : ScriptableObjectIncludeEnum<ColorSelectType>
    {
        [Serializable]
        public class ColorSelectInfo
        {
            public ItemSubType itemSubType;
            public List<ColorSelect> colorSelect;
        }

        public List<ColorSelectInfo> colorSelectList;
    }
}
