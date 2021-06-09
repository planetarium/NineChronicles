using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.UI
{
    public class QuitPopup : PopupWidget
    {
        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private EventListener characterSelectEventListener = null;

        [SerializeField]
        private EventListener quitEventListener = null;

        [SerializeField]
        private EventListener closeEventListener = null;

        protected override void Awake()
        {
            base.Awake();
            characterSelectEventListener.AddEvent("Click", SelectCharacter);
            quitEventListener.AddEvent("Click", Quit);
            closeEventListener.AddEvent("Click", () => Close());
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
