using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.UI
{
    public class Title : ScreenWidget
    {
        private bool _ready;
        public Animator animator;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = () =>
            {
                EventSystem.current.SetSelectedGameObject(null);
                OnClick();
            };
        }

        public void Show(string keyStorePath, string privateKey)
        {
            base.Show();
            animator.enabled = false;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
        }

        public void OnClick()
        {
            if (!_ready)
                return;

            var w = Find<LoginPopup>();
            w.Show(_keyStorePath, _privateKey);
            if (w.State.Value == LoginPopup.States.Show)
            {
                animator.gameObject.SetActive(false);
            }
        }

        public void Ready()
        {
            _ready = true;
            animator.enabled = true;
        }
    }
}
