using System.Collections.Generic;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Game.Controller
{
    public static class Palette
    {
        private static List<Color> _colors;

        private static List<Color> Colors
        {
            get
            {
                if (_colors == null)
                {
                    var colorRef = Resources.Load<PaletteScriptableObject>(
                        "ScriptableObject/UI_Palette");
                    _colors = colorRef.palette;
                }

                return _colors;
            }
        }

        public static Color GetColor(int index)
        {
            return Colors[index] != null ? Colors[index] : Color.black;
        }
    }
}
