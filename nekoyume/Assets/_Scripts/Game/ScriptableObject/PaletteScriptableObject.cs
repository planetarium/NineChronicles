using Nekoyume.EnumType;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_Palette", menuName = "Scriptable Object/Palette",
        order = int.MaxValue)]
    public class PaletteScriptableObject : ScriptableObject
    {
        [Serializable]
        public class ButtonColorInfo
        {
            public ButtonColorType ButtonColorType;
            public Color Color;
        }

        public List<ButtonColorInfo> ButtonColorPalette;
    }
}
