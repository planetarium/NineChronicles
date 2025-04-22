using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ChatPopup : PopupWidget
    {
        [SerializeField] private Button confirm;
        [SerializeField] private Button cancel;

        public override void Initialize()
        {
            confirm.onClick.AddListener(() =>
            {
                Helper.Util.OpenURL(Game.LiveAsset.GameConfig.DiscordLink);
                Close(true);
            });

            cancel.onClick.AddListener(() => { Close(true); });

            base.Initialize();
        }

        protected override void OnEnable()
        {
            SubmitWidget = () =>
            {
                Helper.Util.OpenURL(Game.LiveAsset.GameConfig.DiscordLink);
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
