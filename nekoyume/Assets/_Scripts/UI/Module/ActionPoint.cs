using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

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

        [SerializeField]
        private bool syncWithAvatarState = true;

        [SerializeField]
        private EventTrigger eventTrigger = null;

        [SerializeField]
        private GameObject loading;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private int _currentActionPoint;

        public bool IsRemained => _currentActionPoint > 0;

        public Image Image => image;

        public bool NowCharging => loading.activeSelf;

        #region Mono

        private void Awake()
        {
            sliderAnimator.OnSliderChange
                .Subscribe(_ => OnSliderChange())
                .AddTo(gameObject);
            sliderAnimator.SetMaxValue(States.Instance.GameConfigState.ActionPointMax);
            sliderAnimator.SetValue(0f, false);

            GameConfigStateSubject.GameConfigState
                .Subscribe(state => sliderAnimator.SetMaxValue(state.ActionPointMax))
                .AddTo(gameObject);

            GameConfigStateSubject.ActionPointState.ObserveAdd().Subscribe(x =>
            {
                var address = States.Instance.CurrentAvatarState.address;
                if (x.Key == address)
                {
                    Charger(true);
                }

            }).AddTo(gameObject);

            GameConfigStateSubject.ActionPointState.ObserveRemove().Subscribe(x =>
            {
                var address = States.Instance.CurrentAvatarState.address;
                if (x.Key == address)
                {
                    Charger(false);
                }
            }).AddTo(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!syncWithAvatarState)
                return;

            if (!(States.Instance.CurrentAvatarState is null))
            {
                SetActionPoint(States.Instance.CurrentAvatarState.actionPoint, false);
            }

            ReactiveAvatarState.ActionPoint
                .Subscribe(x => SetActionPoint(x, true))
                .AddTo(_disposables);

            OnSliderChange();

            if (States.Instance.CurrentAvatarState is null)
            {
                Charger(false);
            }
            else
            {
                var address = States.Instance.CurrentAvatarState.address;
                if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
                {
                    var value = GameConfigStateSubject.ActionPointState[address];
                    Charger(value);
                }
                else
                {
                    Charger(false);
                }
            }

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
            {
                return;
            }

            _currentActionPoint = actionPoint;
            sliderAnimator.SetValue(_currentActionPoint, useAnimation);
        }

        private void OnSliderChange()
        {
            var current = ((int)sliderAnimator.Value).ToString("N0", CultureInfo.CurrentCulture);
            var max = ((int)sliderAnimator.MaxValue).ToString("N0", CultureInfo.CurrentCulture);
            text.text = $"{current}/{max}";
        }

        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>()
                .Show("UI_BLESS_OF_GODDESS", "UI_BLESS_OF_GODDESS_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }

        public void SetActionPoint(int actionPoint)
        {
            SetActionPoint(actionPoint, false);
        }

        public void SetEventTriggerEnabled(bool value)
        {
            eventTrigger.enabled = value;
        }

        private void Charger(bool isCharging)
        {
            loading.SetActive(isCharging);
            text.enabled = !isCharging;
        }
    }
}
