using System.Security.Cryptography;
using Libplanet;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class VersionInfo : SystemInfoWidget
    {
        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;
        private HashDigest<SHA256> _hash;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);
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

        private void SubscribeBlockHash(HashDigest<SHA256> hash)
        {
            _hash = hash;
            UpdateText();
        }

        private void UpdateText()
        {
            informationText.text = $"APV: {_version} / #{_blockIndex} / Hash: {_hash.ToString().Substring(0, 4)}";
        }
    }
}
