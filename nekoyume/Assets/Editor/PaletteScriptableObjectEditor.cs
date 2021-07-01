using Nekoyume;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(PaletteScriptableObject))]
    public class PaletteScriptableObjectEditor : EnumGeneratorEditor<ColorType>
    {
    }
}
