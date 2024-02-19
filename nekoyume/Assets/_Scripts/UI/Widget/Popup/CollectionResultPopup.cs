using System;
using System.Collections;
using System.Text;
using Nekoyume.L10n;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class CollectionResultPopup : PopupWidget
    {
        [Serializable]
        private class CollectionEffectStat
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private TextMeshProUGUI continueText;

        [SerializeField]
        private TextMeshProUGUI collectionText;

        [SerializeField]
        private CollectionEffectStat[] collectionEffectStats;

        [SerializeField]
        private TextMeshProUGUI collectionCountText;

        [SerializeField]
        private TextMeshProUGUI collectionCountMaxText;

        [SerializeField]
        private CPScreen cpScreen;

        [SerializeField]
        private Button closeButton;

        private Coroutine _timerCoroutine;
        private const float ContinueTime = 3f;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
        }

        public void Show(
            CollectionSheet.Row row,
            (int count, int maxCount) completionRate,
            (int previousCp, int currentCp) cp,
            bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                Debug.LogError(sb.ToString());
                return;
            }

            continueText.alpha = 0f;

            collectionText.text = L10nManager.LocalizeCollectionName(row.Id);

            var statModifiers = row.StatModifiers;
            for (var i = 0; i < collectionEffectStats.Length; i++)
            {
                collectionEffectStats[i].gameObject.SetActive(i < statModifiers.Count);
                if (i < statModifiers.Count)
                {
                    collectionEffectStats[i].text.text = statModifiers[i].StatModifierToString();
                }
            }

            var (count, maxCount) = completionRate;
            collectionCountText.text = count.ToString();
            collectionCountMaxText.text = $"/ {maxCount}";

            var (previousCp, currentCp) = cp;
            cpScreen.Show(previousCp, currentCp);

            base.Show(ignoreShowAnimation);

            StartContinueTimer();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopContinueTimer();

            base.Close(ignoreCloseAnimation);
        }

        private void StopContinueTimer()
        {
            if (_timerCoroutine is not null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void StartContinueTimer()
        {
            if (_timerCoroutine is not null)
            {
                StopCoroutine(_timerCoroutine);
            }

            _timerCoroutine = StartCoroutine(CoContinueTimer(ContinueTime));
        }

        private IEnumerator CoContinueTimer(float timer)
        {
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.alpha = 1f;

            yield return new WaitForSeconds(1.5f);

            var prevFlooredTime = Mathf.Round(timer);
            while (timer >= .3f)
            {
                // 텍스트 업데이트 횟수를 줄이기 위해 소숫점을 내림해
                // 정수부만 체크 후 텍스트 업데이트 여부를 결정합니다.
                var flooredTime = Mathf.Floor(timer);
                if (flooredTime < prevFlooredTime)
                {
                    prevFlooredTime = flooredTime;
                    continueText.text = string.Format(format, flooredTime);
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            Close();
            _timerCoroutine = null;
        }
    }
}
