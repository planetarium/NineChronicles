using Nekoyume.Game;
using Nekoyume.L10n;
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

        protected override void Awake()
        {
            base.Awake();
            characterSelectEventSubject.GetEvent("Click")
                .Subscribe(_ => SelectCharacter())
                .AddTo(gameObject);
            quitEventSubject.GetEvent("Click")
                .Subscribe(_ => Quit())
                .AddTo(gameObject);
            closeEventSubject.GetEvent("Click")
                .Subscribe(_ => Close())
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
            Find<Login>().Show();
            Find<Menu>().Close();
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
