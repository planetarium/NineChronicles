using Nekoyume;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(CostumeColorScriptableObject))]
    public class ColorSelectScriptableObjectEditor : EnumGeneratorEditor<ColorSelectType>
    {
    }
}
