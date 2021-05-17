using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using UnityEngine;
using TMPro;

namespace Nekoyume.Game.Controller
{
    public static class Palette
    {
        private static Dictionary<ButtonColorType, Color> _buttonColorMap;

        private static Dictionary<ButtonColorType, Color> ButtonColorMap
        {
            get
            {
                if (_buttonColorMap == null)
                {
                    var colorRef = Resources.Load<PaletteScriptableObject>(
                        "ScriptableObject/UI_Palette");
                    _buttonColorMap = colorRef.ButtonColorPalette.ToDictionary(c => c.ButtonColorType, c => c.Color);
                }

                return _buttonColorMap;
            }
        }

        public static Color GetButtonColor(ButtonColorType colorType)
        {
            return ButtonColorMap.ContainsKey(colorType) ? ButtonColorMap[colorType] : Color.black;
        }
    }
}
