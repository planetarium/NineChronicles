using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_Palette", menuName = "Scriptable Object/Palette",
        order = int.MaxValue)]
    public class PaletteScriptableObject : ScriptableObject
    {
        public List<Color> palette;
    }
}
