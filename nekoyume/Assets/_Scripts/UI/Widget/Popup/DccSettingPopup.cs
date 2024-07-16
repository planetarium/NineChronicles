using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DccSettingPopup : PopupWidget
    {
        [SerializeField]
        private Button dccButton;

        [SerializeField]
        private Button openSeaButton;

        [SerializeField]
        private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            dccButton.onClick.AddListener(() => { Find<ConfirmConnectPopup>().ShowConnectDcc(); });
            openSeaButton.onClick.AddListener(() => { Find<ConfirmConnectPopup>().ShowConnectOpenSea(); });
            closeButton.onClick.AddListener(() => { Close(); });

            CloseWidget = () => Close();
        }
    }
}
