using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    public class AlphaAnimateModule : MonoBehaviour
    {
        [SerializeField]
        protected CanvasGroup canvasGroup = null;

        [SerializeField]
        private bool animateAlpha = false;

        private Tweener _tweener;

        protected virtual void OnEnable()
        {
            if (animateAlpha)
            {
                canvasGroup.alpha = 0f;
                _tweener = canvasGroup.DOFade(1, 1f);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }

        protected virtual void OnDisable()
        {
            _tweener?.Kill();
        }
    }
}
