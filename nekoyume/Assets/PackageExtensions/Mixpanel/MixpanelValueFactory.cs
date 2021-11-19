using mixpanel;
using UnityEngine;

namespace PackageExtensions.Mixpanel
{
    public static class MixpanelValueFactory
    {
        private static bool _initialized;
        private static string _clientHost;
        private static string _clientHash;

        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _clientHost = Resources.Load<TextAsset>("MixpanelClientHost").text;
            _clientHash = Resources.Load<TextAsset>("MixpanelClientHash").text;
            _initialized = true;
        }

        public static Value GetValue(params (string key, string value)[] properties)
        {
            Initialize();

            var value = new Value
            {
                ["client-host"] = _clientHost,
                ["client-hash"] = _clientHash,
            };

            foreach (var (key, v) in properties)
            {
                value[key] = v;
            }

            return value;
        }
    }
}
