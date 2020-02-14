using System;
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
        public RectTransform tooltipArea;
        public CanvasGroup canvasGroup;
        public bool animateAlpha;

        private IDisposable _disposable;
        private VanilaTooltip _tooltip;

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
            text.text = $"{actionPoint} / {slider.maxValue}";
            slider.value = actionPoint;
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
