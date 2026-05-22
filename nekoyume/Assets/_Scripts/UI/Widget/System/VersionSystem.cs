using Libplanet.Types.Blocks;
using Nekoyume.Blockchain;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class VersionSystem : SystemWidget
    {
        private const string RpcHostSuffix = ".nine-chronicles.com";

        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;
        private BlockHash _hash;
        private string _clientCommitHash;
        private string _nodeName;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);

            if (Game.Game.instance.Agent is RPCAgent rpcAgent)
            {
                rpcAgent.CurrentRpcServerHost.Subscribe(SubscribeRpcServerHost).AddTo(gameObject);
            }

            _clientCommitHash = Resources.Load<TextAsset>("ClientHash")?.text[..8] ?? string.Empty;

            UpdateText();
        }

        public void SetVersion(int version)
        {
            _version = version;
            UpdateText();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateText();
        }

        private void SubscribeBlockHash(BlockHash hash)
        {
            _hash = hash;
            UpdateText();
        }

        private void SubscribeRpcServerHost(string host)
        {
            _nodeName = string.IsNullOrEmpty(host)
                ? string.Empty
                : host.EndsWith(RpcHostSuffix)
                    ? host[..^RpcHostSuffix.Length]
                    : host;
            UpdateText();
        }

        private void UpdateText()
        {
            var hash = _hash.ToString();
            hash = hash.Length >= 4 ? hash.Substring(0, 4) : "...";

            var versionText = string.Empty;
            var commitHashText = string.Empty;
            if (!string.IsNullOrEmpty(_clientCommitHash))
            {
                commitHashText = $"({_clientCommitHash})";
            }

            versionText = $"/ Ver: {Application.version}{commitHashText}";

            var nodeText = string.IsNullOrEmpty(_nodeName)
                ? string.Empty
                : $" / Node: {_nodeName}";

            informationText.text = $"APV: {_version} / #{_blockIndex} / Hash: {hash} {versionText}{nodeText}";
        }
    }
}
