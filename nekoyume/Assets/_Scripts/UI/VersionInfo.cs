using Libplanet;
using Libplanet.Blocks;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class VersionInfo : SystemInfoWidget
    {
        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;
        private BlockHash _hash;

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

        private void SubscribeBlockHash(BlockHash hash)
        {
            _hash = hash;
            UpdateText();
        }

        private void UpdateText()
        {
            string format = "APV: {0} / #{1} / Hash: {2}";
            format = "PandoraBox: v1.0 / #{1} / Hash: {2}";
            var hash = _hash.ToString();
            var text = string.Format(
                format,
                _version,
                _blockIndex,
                hash.Length >= 4 ? hash.Substring(0, 4) : "...");
            informationText.text = text;
        }
    }
}
