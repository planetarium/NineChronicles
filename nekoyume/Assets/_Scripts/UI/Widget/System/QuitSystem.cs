using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using UnityEngine;
using UniRx;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class QuitSystem : SystemWidget
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
                .Subscribe(_ =>
                {
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

            CloseWidget = () =>
            {
                Close();
            };
        }

        public void Show(float blurRadius = 2, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            blur.Show(blurRadius);
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
