using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationLoadingScreen : ScreenWidget
    {
        private const float AlphaToBeginning = 0.5f;

        public Image loadingImage;

        private Color _color;
        private Sequence _sequence;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _color = loadingImage.color;
            _color.a = AlphaToBeginning;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            loadingImage.color = _color;
            _sequence = DOTween.Sequence()
                .Append(loadingImage.DOFade(1f, 0.3f))
                .Append(loadingImage.DOFade(AlphaToBeginning, 0.6f))
                .SetLoops(3)
                .OnComplete(() => Close());
        }

        protected override void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            base.OnDisable();
        }

        #endregion
    }
}
