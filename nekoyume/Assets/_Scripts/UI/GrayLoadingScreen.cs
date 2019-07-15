using System;
using Assets.SimpleLocalization;
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

            switch (LocalizationManager.Language)
            {
                case LocalizationManager.LanguageType.English:
                    // ToDo. 영어 이미지.
                    break;
                case LocalizationManager.LanguageType.Korean:
                    // ToDo. 한글 이미지.
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
