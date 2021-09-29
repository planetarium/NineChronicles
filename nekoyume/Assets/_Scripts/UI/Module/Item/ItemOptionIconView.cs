using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemOptionIconView : MonoBehaviour
    {
        [SerializeField]
        private Behaviour _uiHsvModifier;

        [SerializeField]
        private Image _foreground;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private ItemOptionIconViewSO _so;

        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");

        public void Show(bool ignoreAnimation = false)
        {
            gameObject.SetActive(true);
            _animator.Play(AnimatorHashShow, 0, ignoreAnimation ? 1f : 0f);
        }

        public void Hide(bool ignoreAnimation = false)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            _animator.SetTrigger(AnimatorHashHide);
        }

        public void UpdateAsStat()
        {
            _uiHsvModifier.enabled = false;
            _foreground.color = _so.statForegroundColor;
        }

        public void UpdateAsSkill()
        {
            _uiHsvModifier.enabled = true;
            _foreground.color = _so.skillForegroundColor;
        }
        
        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
        }

        public void OnAnimatorStateEnd(string stateName)
        {
            switch (stateName)
            {
                case "Hide":
                    gameObject.SetActive(false);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode) =>
            AudioController.instance.PlaySfx(sfxCode);

        #endregion
    }
}
