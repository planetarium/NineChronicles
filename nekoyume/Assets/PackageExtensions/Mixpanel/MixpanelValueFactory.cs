using mixpanel;
using UnityEngine;

namespace PackageExtensions.Mixpanel
{
    public class MixpanelValueFactory
    {
        private const string _clientHostKey = "client-host";
        private const string _clientHashKey = "client-hash";
        private const string _targetNetworkKey = "target-network";
        private const string _rpcServerHostKey = "rpc-server-host";
        private const string _agentAddressKey = "AgentAddress";

        private readonly string _clientHost;
        private readonly string _clientHash;
        private readonly string _targetNetwork;
        private readonly string _rpcServerHost;
        private readonly string _agentAddress;

        public MixpanelValueFactory(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost = null,
            string agentAddress = null)
        {
            _clientHost = clientHost;
            _clientHash = clientHash;
            _targetNetwork = targetNetwork;
            _rpcServerHost = rpcServerHost;
            _agentAddress = agentAddress;

            Debug.Log(
                $"[{nameof(MixpanelValueFactory)}] Initialized. {_clientHost} {_clientHash} {_rpcServerHost ?? ""} {_agentAddress ?? ""}");
        }

        public Value GetValue(params (string key, string value)[] properties)
        {
            var result = new Value
            {
                [_clientHostKey] = _clientHost,
                [_clientHashKey] = _clientHash,
                [_targetNetworkKey] = _targetNetwork,
            };

            if (_rpcServerHost is { })
            {
                result[_rpcServerHostKey] = _rpcServerHost;
            }

            if (_agentAddress is { })
            {
                result[_agentAddressKey] = _agentAddress;
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
            value[_targetNetworkKey] = _targetNetwork;

            if (_rpcServerHost is { })
            {
                value[_rpcServerHostKey] = _rpcServerHost;
            }

            if (_agentAddress is { })
            {
                value[_agentAddressKey] = _agentAddress;
            }

            return value;
        }
    }
}
