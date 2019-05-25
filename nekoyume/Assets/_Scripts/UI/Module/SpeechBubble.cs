using Assets.SimpleLocalization;
using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI.Module
{
    public class SpeechBubble : MonoBehaviour
    {
        public string localizationKey;
        public Image bubbleImage;
        public Text text;
        public string imageName;
        public Vector3 positionOffset;
        public float speechSpeedInterval = 0.04f;
        public float speechWaitTime = 2.0f;
        public float bubbleTweenTime = 0.2f;

        private int _speechCount = 0;

        public void Init()
        {
            _speechCount = LocalizationManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public IEnumerator Show()
        {
            if (_speechCount == 0)
                yield break;

            gameObject.SetActive(true);

            bubbleImage.rectTransform.DOScale(0.0f, 0.0f);
            bubbleImage.rectTransform.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(bubbleTweenTime);

            string speech = LocalizationManager.Localize($"{localizationKey}{Random.Range(0, _speechCount)}");
            for (int i = 1; i <= speech.Length; ++i)
            {
                if (i == speech.Length)
                    text.text = $"{speech.Substring(0, i)}";
                else
                    text.text = $"{speech.Substring(0, i)}<color=white>{speech.Substring(i)}</color>";
                yield return new WaitForSeconds(speechSpeedInterval);
            }

            yield return new WaitForSeconds(speechWaitTime);

            text.text = "";
            bubbleImage.rectTransform.DOScale(0.0f, bubbleTweenTime).SetEase(Ease.InBack);
            yield return new WaitForSeconds(bubbleTweenTime);

            gameObject.SetActive(false);
        }

        public void Hide()
        {
            text.text = "";
            gameObject.SetActive(false);
        }
    }
}
