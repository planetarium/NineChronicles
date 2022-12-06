using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "NoticeEndpoint", menuName = "Scriptable Object/Notice Endpoint ScriptableObject",
        order = int.MaxValue)]
    public class NoticeEndpointScriptableObject : UnityEngine.ScriptableObject
    {
        [field: SerializeField]
        public string EventJsonUrl { get; private set; }

        [field: SerializeField]
        public string NoticeJsonUrl { get; private set; }

        [field: SerializeField]
        public string ImageRootUrl { get; private set; }
    }
}
