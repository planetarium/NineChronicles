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

        private const string DccURL = "https://dcc.nine-chronicles.com/";
        private const string OpenSeaURL = "https://opensea.io/collection/dcc-ninechronicles";

        protected override void Awake()
        {
            base.Awake();

            dccButton.onClick.AddListener(() =>
            {
                Application.OpenURL(DccURL);
            });

            openSeaButton.onClick.AddListener(() =>
            {
                Application.OpenURL(OpenSeaURL);
            });

            CloseWidget = () => { Close(); };
        }
    }
}
