using System;
using Nekoyume.Model;
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

        private IDisposable _disposable;

        #region Mono

        private void Awake()
        {
            const long MaxValue = GameConfig.ActionPoint;
            slider.maxValue = MaxValue;
            text.text = $"{MaxValue} / {MaxValue}";
        }

        private void OnEnable()
        {
            _disposable = ReactiveCurrentAvatarState.ActionPoint.Subscribe(SetPoint);
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
    }
}
