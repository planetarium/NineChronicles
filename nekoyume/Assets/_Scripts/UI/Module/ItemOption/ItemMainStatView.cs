using Nekoyume.Model.Stat;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemMainStatView : ItemOptionView
    {
        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Animator _animator;

        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");

        public override void Show(bool ignoreAnimation = false)
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

        public void UpdateView(StatType type, int totalValue, int plusValue) =>
            UpdateView($"{type.ToString()} +{totalValue}", $"+{plusValue}");
    }
}
