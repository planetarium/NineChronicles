using System.Collections.Generic;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Game.Controller
{
    public class ColorController : MonoSingleton<ColorController>
    {
        private List<Color> _colors;

        public void Initialize()
        {
            var colorRef = Resources.Load<ColorReferenceScriptableObject>("ScriptableObject/UI_ColorReference");
            _colors = colorRef.datas;
        }

        public static Color Color(int index)
        {
            return instance._colors[index] != null
                ? instance._colors[index]
                : UnityEngine.Color.black;
        }
    }
}
