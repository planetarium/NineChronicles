using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.Game;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class SpeechBubble : HudWidget
    {
        public enum Type
        {
            Room,
            World,
        }

        public string localizationKey;
        public Image bubbleImage;
        public Text textSize;
        public Text text;
        public float speechSpeedInterval = 0.04f;
        public float speechWaitTime = 2.0f;
        public float bubbleTweenTime = 0.2f;

        public float speechBreakTime = 0.0f;
        public float destroyTime = 4.0f;

        private int _speechCount = 0;

        public void Init()
        {
            _speechCount = LocalizationManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition = targetPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
        }

        public bool SetKey(string value)
        {
            localizationKey = value;
            _speechCount = LocalizationManager.LocalizedCount(localizationKey);
            return _speechCount > 0;
        }

        public IEnumerator CoShowText()
        {
            if (_speechCount == 0)
                yield break;

            gameObject.SetActive(true);

            string speech = LocalizationManager.Localize($"{localizationKey}{Random.Range(0, _speechCount)}");
            if (!string.IsNullOrEmpty(speech))
            {
                textSize.text = speech;
                textSize.rectTransform.DOScale(0.0f, 0.0f);
                textSize.rectTransform.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(bubbleTweenTime);
                for (int i = 1; i <= speech.Length; ++i)
                {
                    if (i == speech.Length)
                        text.text = $"{speech.Substring(0, i)}";
                    else
                        text.text = $"{speech.Substring(0, i)}<color=#ffffff00>{speech.Substring(i)}</color>";
                    yield return new WaitForSeconds(speechSpeedInterval);

                    // check destroy
                    if (!gameObject)
                    {
                        break;
                    }
                }

                yield return new WaitForSeconds(speechWaitTime);

                text.text = "";
                textSize.rectTransform.DOScale(0.0f, bubbleTweenTime).SetEase(Ease.InBack);
                yield return new WaitForSeconds(bubbleTweenTime);
            }
            
            yield return new WaitForSeconds(speechBreakTime);

            gameObject.SetActive(false);
        }

        public void Hide()
        {
            text.text = "";
            gameObject.SetActive(false);
        }
    }
}
