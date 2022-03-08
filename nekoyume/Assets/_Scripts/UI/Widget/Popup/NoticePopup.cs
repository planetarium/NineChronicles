using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI
{
    public class NoticePopup : PopupWidget
    {
        [SerializeField] private Blur blur;
        [SerializeField] private Button detailButton;
        [SerializeField] private Button closeButton;
        
        private const string NoticePageUrlFormat = "https://www.notion.so/planetarium/1bc6de399b3b4ace95fca3a3020b4d79";

        protected override void Awake()
        {
            base.Awake();

            detailButton.onClick.AddListener(() =>
            {
                GoToNoticePage();
                AudioController.PlayClick();
            });
            
            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            blur.button.onClick.AddListener(() => Close(true));
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);

            if (blur)
            {
                blur.Show();
            }
            // HelpTooltip.HelpMe(100014, true);
        }
        
        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur && blur.isActiveAndEnabled)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }

        private static void GoToNoticePage()
        {
            Application.OpenURL(NoticePageUrlFormat);
        }
    }
}
