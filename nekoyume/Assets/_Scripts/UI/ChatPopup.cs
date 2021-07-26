
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ChatPopup : PopupWidget
    {
        [SerializeField] private Button confirm;
        [SerializeField] private Button cancel;
        [SerializeField] private Button close;

        protected override void Awake()
        {
            confirm.onClick.AddListener(() =>
            {
                Application.OpenURL(GameConfig.DiscordLink);
                HelpPopup.HelpMe(100012, true);
                Close(true);
            });

            cancel.onClick.AddListener(() =>
            {
                Close(true);
            });

            close.onClick.AddListener(() =>
            {
                Close(true);
            });
            base.Awake();
        }

        protected override void OnEnable()
        {
            SubmitWidget = () =>
            {
                Application.OpenURL(GameConfig.DiscordLink);
                HelpPopup.HelpMe(100012, true);
                Close(true);
            };

            CloseWidget = () => Close(true);
            base.OnEnable();
        }


        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(true);
        }
    }
}
