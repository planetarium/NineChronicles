using System;
using System.Collections;
using DG.Tweening;
using Nekoyume.UI.Tween;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Common
{
    [Serializable]
    public class SliderAnimator : ISerializationCallbackReceiver
    {
        [SerializeField]
        protected Slider slider = null;

        private Tweener _tweener;

        private readonly Subject<float> _onMaxValueChange = new Subject<float>();
        private readonly Subject<float> _onValueChange = new Subject<float>();

        public bool IsFull => Math.Abs(Value - MaxValue) < 0.001f;
        public IObservable<float> OnMaxValueChange => _onMaxValueChange;
        public IObservable<float> OnValueChange => _onValueChange;

        public IObservable<SliderAnimator> OnSliderChange => OnMaxValueChange
            .Merge(OnValueChange)
            .Select(x => this);

        public float MaxValue
        {
            get => slider.maxValue;
            private set
            {
                slider.maxValue = value;
                _onMaxValueChange.OnNext(slider.maxValue);
            }
        }

        public float Value
        {
            get => slider.value;
            private set
            {
                slider.value = value;
                _onValueChange.OnNext(slider.value);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (slider is null)
            {
                throw new SerializeFieldNullException();
            }
        }

        public void SetMaxValue(float value)
        {
            MaxValue = value;
        }

        public void SetValue(float value, bool useAnimation = true)
        {
            Stop();

            if (useAnimation)
            {
                _tweener = DOTween
                    .To(() => Value, x => Value = x, value, 0.5f)
                    .Play();
            }
            else
            {
                Value = value;
            }
        }

        public void Stop()
        {
            if (!(_tweener is null) &&
                _tweener.IsActive() &&
                _tweener.IsPlaying())
            {
                _tweener.Kill();
                _tweener = null;
            }
        }
    }
}
