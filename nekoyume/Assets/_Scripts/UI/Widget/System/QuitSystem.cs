using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UniRx;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class QuitSystem : SystemWidget
    {
        [SerializeField]
        private EventSubject characterSelectEventSubject = null;

        [SerializeField]
        private EventSubject quitEventSubject = null;

        [SerializeField]
        private EventSubject closeEventSubject = null;

        [SerializeField]
        private UIBackground background;

        protected override void Awake()
        {
            base.Awake();
            characterSelectEventSubject.GetEvent("Click")
                .Subscribe(_ =>
                {
                    var address = States.Instance.CurrentAvatarState.address;
                    if (WorldBossStates.IsReceivingGradeRewards(address))
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_CAN_NOT_CHANGE_CHARACTER"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    Game.Game.instance.BackToNest();
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
            quitEventSubject.GetEvent("Click")
                .Subscribe(_ =>
                {
                    Quit();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
            closeEventSubject.GetEvent("Click")
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);

            CloseWidget = () => { Close(); };

            background.OnClick = CloseWidget;
        }

        public void Show(float blurRadius = 2, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            AudioController.PlayPopup();
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
