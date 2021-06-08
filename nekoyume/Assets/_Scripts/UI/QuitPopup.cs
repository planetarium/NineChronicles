using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuitPopup : PopupWidget
    {
        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private AnimationEventListener characterSelectButtonListener = null;

        [SerializeField]
        private AnimationEventListener quitButtonListener = null;

        [SerializeField]
        private AnimationEventListener closeButtonListener = null;

        protected override void Awake()
        {
            base.Awake();
            characterSelectButtonListener.TryAddCallback("Click", SelectCharacter);
            quitButtonListener.TryAddCallback("Click", Quit);
            closeButtonListener.TryAddCallback("Click", () => Close());
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
