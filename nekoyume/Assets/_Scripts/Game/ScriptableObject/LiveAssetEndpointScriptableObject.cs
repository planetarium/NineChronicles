using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "LiveAssetEndpoint", menuName = "Scriptable Object/LiveAsset Endpoint ScriptableObject",
        order = int.MaxValue)]
    public class LiveAssetEndpointScriptableObject : UnityEngine.ScriptableObject
    {
        [field: SerializeField]
        public string EventJsonUrl { get; private set; }

        [field: SerializeField]
        public string NoticeJsonUrl { get; private set; }

        [field: SerializeField]
        public string NoticeJsonKoreanUrl { get; private set; }

        [field: SerializeField]
        public string NoticeJsonJapaneseUrl { get; private set; }

        [field: SerializeField]
        public string ImageRootUrl { get; private set; }

        [field: SerializeField]
        public string GameConfigJsonUrl { get; private set; }

        [field: SerializeField]
        public string CommandLineOptionsJsonUrlPrefix { get; private set; }

        [field: SerializeField]
        public string CommandLineOptionsJsonDefaultUrl { get; private set; }

        [field: SerializeField]
        public string CommandLineOptionsJsonDefaultUrlKr { get; private set; }
    }
}
