using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrayLoadingScreen : ScreenWidget
    {
        private const float AlphaToBeginning = 0.5f;
        
        public Image tweenAlphaImage;
        
        private Color _color;
        private Sequence _sequence = null;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (tweenAlphaImage == null)
            {
                return;
            }

            _color = tweenAlphaImage.color;
            _color.a = AlphaToBeginning;
        }

        private void OnEnable()
        {
            if (!tweenAlphaImage)
            {
                return;
            }

            tweenAlphaImage.color = _color;
            _sequence = DOTween.Sequence()
                .Append(tweenAlphaImage.DOFade(1f, 0.3f))
                .Append(tweenAlphaImage.DOFade(AlphaToBeginning, 0.6f))
                .SetLoops(-1);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        #endregion
    }
}
