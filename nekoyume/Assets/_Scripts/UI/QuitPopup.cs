using Nekoyume.Game;
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
        }

        public void Show(float blurRadius = 2, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            blur.Show(blurRadius);
        }

        private void SelectCharacter()
        {
            Nekoyume.Game.Event.OnNestEnter.Invoke();
            Find<Login>().Show();
            Find<Menu>().Close();
            Close(true);
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
