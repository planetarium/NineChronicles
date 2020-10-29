using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class VersionInfo : SystemInfoWidget
    {
        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
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

        private void UpdateText()
        {
            informationText.text = $"APV: {_version} / BlockIndex: {_blockIndex}";
        }
    }
}
