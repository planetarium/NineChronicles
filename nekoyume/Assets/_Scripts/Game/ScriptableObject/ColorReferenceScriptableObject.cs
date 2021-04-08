using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_ColorReference", menuName = "Scriptable Object/Color Reference",
        order = int.MaxValue)]
    public class ColorReferenceScriptableObject : ScriptableObject
    {
        public List<Color> datas;
    }
}
