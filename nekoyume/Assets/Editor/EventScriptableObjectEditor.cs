using Nekoyume;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(EventScriptableObject))]
    public class EventScriptableObjectEditor : EnumGeneratorEditor<EventType>
    {

    }
}
