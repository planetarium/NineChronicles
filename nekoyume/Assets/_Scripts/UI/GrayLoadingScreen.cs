using System;
using System.Collections;
using Assets.SimpleLocalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrayLoadingScreen : ScreenWidget
    {
        private const float AlphaToBeginning = 0.5f;
        
        public Image koreanImage;
        public GameObject englishImageContainer;
        public TextMeshProUGUI englishText;
        
        private Color _color;
        private Sequence _sequence;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (koreanImage == null)
            {
                return;
            }

            switch (LocalizationManager.CurrentLanguage)
            {
                case LocalizationManager.LanguageType.English:
                    koreanImage.gameObject.SetActive(false);
                    englishImageContainer.gameObject.SetActive(true);
                    englishText.gameObject.SetActive(true);
                    break;
                case LocalizationManager.LanguageType.Korean:
                    koreanImage.gameObject.SetActive(true);
                    englishImageContainer.gameObject.SetActive(false);
                    englishText.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _color = koreanImage.color;
            _color.a = AlphaToBeginning;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            switch (LocalizationManager.CurrentLanguage)
            {
                case LocalizationManager.LanguageType.English:
                    OnEnableAsEnglish();
                    break;
                case LocalizationManager.LanguageType.Korean:
                    OnEnableAsKorean();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            
            base.OnDisable();

        }

        #endregion

        private void OnEnableAsEnglish()
        {
            // ToDo. 연출은 피드백 받은 후에 진행.            
        }
        
        private void OnEnableAsKorean()
        {
            if (!koreanImage)
            {
                return;
            }
            
            koreanImage.color = _color;
            _sequence = DOTween.Sequence()
                .Append(koreanImage.DOFade(1f, 0.3f))
                .Append(koreanImage.DOFade(AlphaToBeginning, 0.6f))
                .SetLoops(-1);
        }
    }
}
