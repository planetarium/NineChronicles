using mixpanel;
using UnityEngine;

namespace PackageExtensions.Mixpanel
{
    public class MixpanelValueFactory
    {
        private const string _clientHostKey = "client-host";
        private const string _clientHashKey = "client-hash";
        
        private bool _initialized;
        private string _clientHost;
        private string _clientHash;

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _clientHost = Resources.Load<TextAsset>("MixpanelClientHost")?.text ?? "no-host";
            _clientHash = Resources.Load<TextAsset>("MixpanelClientHash")?.text ?? "no-hash";
            _initialized = true;
            Debug.Log($"[{nameof(MixpanelValueFactory)}] Initialized. {_clientHost} {_clientHash}");
        }

        public Value GetValue(params (string key, string value)[] properties)
        {
            Initialize();

            var result = new Value
            {
                [_clientHostKey] = _clientHost,
                [_clientHashKey] = _clientHash,
            };

            foreach (var (key, value) in properties)
            {
                result[key] = value;
            }

            return result;
        }

        public Value UpdateValue(Value value)
        {
            value[_clientHostKey] = _clientHost;
            value[_clientHashKey] = _clientHash;
            return value;
        }
    }
}
