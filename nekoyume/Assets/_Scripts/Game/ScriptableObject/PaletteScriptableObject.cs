using Nekoyume.EnumType;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_Palette", menuName = "Scriptable Object/Palette",
        order = int.MaxValue)]
    public class PaletteScriptableObject : ScriptableObject
    {
        [Serializable]
        private class ColorInfo
        {
            public ButtonColorType Name;
            public Color Color;
        }

        [SerializeField]
        private List<ColorInfo> buttonColorPalette;

        public List<Color> palette;
    }


}
