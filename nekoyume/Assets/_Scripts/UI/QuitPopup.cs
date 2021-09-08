using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using UnityEngine;
using UniRx;

namespace Nekoyume.UI
{
    public class QuitPopup : PopupWidget
    {
        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private EventSubject characterSelectEventSubject = null;

        [SerializeField]
        private EventSubject quitEventSubject = null;

        [SerializeField]
        private EventSubject closeEventSubject = null;

        public override WidgetType WidgetType => WidgetType.SystemInfo;

        protected override void Awake()
        {
            base.Awake();
            characterSelectEventSubject.GetEvent("Click")
                .Subscribe(_ =>
                {
                    SelectCharacter();
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

            CloseWidget = () =>
            {
                Close();
            };
        }

        public void Show(float blurRadius = 2, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            blur.Show(blurRadius);
        }

        private void SelectCharacter()
        {
            if (Game.Game.instance.Stage.IsInStage)
            {
                Notification.Push(Nekoyume.Model.Mail.MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"));
                return;
            }

            Game.Event.OnNestEnter.Invoke();

            var deletableWidgets = FindWidgets().Where(widget =>
                !(widget is SystemInfoWidget) && !(widget is QuitPopup) &&
                !(widget is MessageCatManager) && widget.IsActive());
            foreach (var widget in deletableWidgets)
            {
                widget.Close(true);
            }
            Find<Login>().Show();
            Close();
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
