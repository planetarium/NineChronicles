using System;
using System.Collections;
using DG.Tweening;
using Nekoyume.Model;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ActionPoint : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Slider slider;
        public Image image;
        public RectTransform tooltipArea;
        public CanvasGroup canvasGroup;
        public bool animateAlpha;

        private IDisposable _disposable;
        private VanilaTooltip _tooltip;
        private Coroutine lerpCoroutine;

        #region Mono

        private void Awake()
        {
            const long MaxValue = GameConfig.ActionPointMax;
            slider.maxValue = MaxValue;
            text.text = $"{MaxValue} / {MaxValue}";
        }

        private void OnEnable()
        {
            _disposable = ReactiveAvatarState.ActionPoint.Subscribe(SetPoint);

            if (animateAlpha)
            {
                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1, 1.0f);
            }
        }

        private void OnDisable()
        {
            _disposable.Dispose();
        }

        #endregion

        private void SetPoint(int actionPoint)
        {
            if(lerpCoroutine != null)
                StopCoroutine(lerpCoroutine);
            
            lerpCoroutine = StartCoroutine(LerpSlider(actionPoint));
        }
        
        private IEnumerator LerpSlider(int value, int additionalSpeed = 1)
        {
            var current = slider.value;
            var speed = 4 * additionalSpeed;

            while (current <= value - 2)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * speed);
                slider.value = current;
                text.text = $"{(int)current} / {GameConfig.ActionPointMax}";
                yield return null;
            }

            slider.value = value;
            text.text = $"{value} / {GameConfig.ActionPointMax}";
        }

        public void ShowTooltip()
        {
            _tooltip = Widget.Find<VanilaTooltip>();
            _tooltip?.Show("UI_BLESS_OF_GODDESS", "UI_BLESS_OF_GODDESS_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            _tooltip?.Close();
            _tooltip = null;
        }
    }
}
