using mixpanel;
using UnityEngine;

namespace PackageExtensions.Mixpanel
{
    public class MixpanelValueFactory
    {
        private const string _clientHostKey = "client-host";
        private const string _clientHashKey = "client-hash";
        private const string _rpcServerHostKey = "rpc-server-host";

        private readonly string _clientHost;
        private readonly string _clientHash;
        private readonly string _rpcServerHost;

        public MixpanelValueFactory(string rpcServerHost = null)
        {
            _clientHost = Resources.Load<TextAsset>("MixpanelClientHost")?.text ?? "no-host";
            _clientHash = Resources.Load<TextAsset>("MixpanelClientHash")?.text ?? "no-hash";
            _rpcServerHost = rpcServerHost;

            Debug.Log(
                $"[{nameof(MixpanelValueFactory)}] Initialized. {_clientHost} {_clientHash} {_rpcServerHost ?? ""}");
        }

        public Value GetValue(params (string key, string value)[] properties)
        {
            var result = new Value
            {
                [_clientHostKey] = _clientHost,
                [_clientHashKey] = _clientHash,
            };

            if (_rpcServerHost is { })
            {
                result[_rpcServerHostKey] = _rpcServerHost;
            }

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

            if (_rpcServerHost is { })
            {
                value[_rpcServerHostKey] = _rpcServerHost;
            }

            return value;
        }
    }
}
