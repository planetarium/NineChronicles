using System;
using System.Collections;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ActionPoint : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Slider slider = null;

        [SerializeField]
        private Image image = null;

        [SerializeField]
        private RectTransform tooltipArea = null;
        
        private Coroutine _coLerpSlider;

        public Image Image => image;

        #region Mono

        private void Awake()
        {
            const long MaxValue = GameConfig.ActionPointMax;
            slider.maxValue = GameConfig.ActionPointMax;
            slider.value = 0;
            text.text = $"0 / {GameConfig.ActionPointMax}";
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (States.Instance.CurrentAvatarState != null)
                SetPoint(States.Instance.CurrentAvatarState.actionPoint);
        }

        #endregion

        public void SetPoint(int actionPoint, bool useAnimation = false)
        {
            if (!gameObject.activeSelf)
                return;

            if (!useAnimation)
            {
                slider.value = actionPoint;
                text.text = $"{actionPoint} / {GameConfig.ActionPointMax}";

                return;
            }

            if (!(_coLerpSlider is null))
            {
                StopCoroutine(_coLerpSlider);
            }

            _coLerpSlider = StartCoroutine(CoLerpSlider(actionPoint));
        }

        private IEnumerator CoLerpSlider(int value, int additionalSpeed = 1)
        {
            var current = slider.value;
            var speed = 4 * additionalSpeed;

            while (current <= value - 2)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * speed);
                slider.value = current;
                text.text = $"{(int) current} / {GameConfig.ActionPointMax}";
                yield return null;
            }

            slider.value = value;
            text.text = $"{value} / {GameConfig.ActionPointMax}";
        }

        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>().Show("UI_BLESS_OF_GODDESS", "UI_BLESS_OF_GODDESS_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }
    }
}
