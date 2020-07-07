using System;
using System.Collections.Generic;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ActionPoint : AlphaAnimateModule
    {
        [SerializeField]
        private SliderAnimator sliderAnimator = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Image image = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private int _currentActionPoint;

        public Image Image => image;

        #region Mono

        private void Awake()
        {
            sliderAnimator.OnSliderChange.Subscribe(_ => OnSliderChange()).AddTo(gameObject);
            sliderAnimator.SetMaxValue(States.Instance.GameConfigState.ActionPointMax);
            GameConfigStateSubject.GameConfigState.ObserveOnMainThread().Subscribe(
                state => sliderAnimator.SetMaxValue(state.ActionPointMax)
            ).AddTo(gameObject);
            sliderAnimator.SetValue(0f, false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!(States.Instance.CurrentAvatarState is null))
            {
                SetActionPoint(States.Instance.CurrentAvatarState.actionPoint, false);
            }

            ReactiveAvatarState.ActionPoint
                .Subscribe(x => SetActionPoint(x, true))
                .AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            sliderAnimator.Stop();
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        #endregion

        private void SetActionPoint(int actionPoint, bool useAnimation)
        {
            if (_currentActionPoint == actionPoint)
                return;

            _currentActionPoint = actionPoint;
            sliderAnimator.SetValue(_currentActionPoint, useAnimation);
        }

        private void OnSliderChange()
        {
            text.text = $"{(int) sliderAnimator.Value} / {sliderAnimator.MaxValue}";
        }

        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>()
                .Show("UI_BLESS_OF_GODDESS", "UI_BLESS_OF_GODDESS_DESCRIPTION", tooltipArea.position);

            HelpPopup.HelpMe(100009, true);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }
    }
}
