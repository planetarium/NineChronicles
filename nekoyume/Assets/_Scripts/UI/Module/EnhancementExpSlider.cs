using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EnhancementExpSlider : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private AnimationCurve sliderEffectCurve;

        [SerializeField]
        private TextMeshProUGUI percentText;

        private long _expAnchorPoint;
        private long _levelAnchorPoint;
        private Coroutine _sliderEffectCor;
        private List<long> _expTable;

        public void SetEquipment(Equipment equipment, bool reset = false)
        {
            if (reset)
            {
                _expAnchorPoint = 0;
                _levelAnchorPoint = 0;
                slider.value = 0;
                percentText.text = "0% 0/0";

                if (_sliderEffectCor != null)
                {
                    StopCoroutine(_sliderEffectCor);
                }
            }

            if (equipment is null)
            {
                return;
            }

            var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;
            _expTable = enhancementCostSheet.Values
                .Where(row => row.Grade == equipment.Grade &&
                    row.ItemSubType == equipment.ItemSubType)
                .Select(row => row.Exp).ToList();
            _expTable.Insert(0, 0);
        }

        public void SliderGageEffect(long targetExp, int targetLevel)
        {
            // default duration by curve
            var duration = sliderEffectCurve.keys.Last().time;
            var levelDiff = Mathf.Abs(_levelAnchorPoint - targetLevel);
            if (levelDiff > 0)
            {
                // extra duration by level difference (1 ~ 3 sec)
                duration += Mathf.Lerp(1f, 3f, levelDiff * 0.05f);
            }

            if (_sliderEffectCor != null)
            {
                StopCoroutine(_sliderEffectCor);
            }

            _sliderEffectCor = StartCoroutine(
                CoroutineEffect(_expAnchorPoint, targetExp, duration));
        }

        private IEnumerator CoroutineEffect(long startExp, long targetExp, float duration)
        {
            var elapsedTime = 0f;
            while (elapsedTime <= duration)
            {
                elapsedTime += Time.deltaTime;

                var progressExp = sliderEffectCurve.Evaluate(elapsedTime / duration);
                SetSliderValue(Mathf.Lerp(startExp, targetExp, progressExp));
                yield return new WaitForEndOfFrame();
            }

            SetSliderValue(targetExp);
        }

        private void SetSliderValue(float exp)
        {
            _expAnchorPoint = (long)exp;
            var (progressSliderValue, nextExp) = ExpToSliderValue();
            slider.value = progressSliderValue;
            percentText.text = $"{(int)(progressSliderValue * 100)}% {exp:N0}/{nextExp:N0}";
            return;

            (float progress, long nextExp) ExpToSliderValue()
            {
                for (var i = 0; i < _expTable.Count; i++)
                {
                    if (_expTable[i] <= exp)
                    {
                        continue;
                    }

                    _levelAnchorPoint = i - 1;
                    return (Mathf.InverseLerp(_expTable[i - 1], _expTable[i], exp), _expTable[i]);
                }

                return (1, _expTable.Last());
            }
        }
    }
}
