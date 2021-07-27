using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using UnityEngine;
using TMPro;

namespace Nekoyume.Game.Controller
{
    public static class Palette
    {
        private static Dictionary<ColorType, Color> _buttonColorMap;

        private static Dictionary<ColorType, Color> ButtonColorMap
        {
            get
            {
                if (_buttonColorMap == null)
                {
                    var colorRef = Resources.Load<PaletteScriptableObject>(
                        "ScriptableObject/UI_Palette");
                    _buttonColorMap = colorRef.Palette.ToDictionary(c => c.colorType, c => c.Color);
                }

                return _buttonColorMap;
            }
        }

        public static Color GetColor(ColorType colorType)
        {
            return ButtonColorMap.ContainsKey(colorType) ? ButtonColorMap[colorType] : Color.black;
        }
    }
}
