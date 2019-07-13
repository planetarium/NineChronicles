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
        public enum ImageType : int
        {
            Normal,
            Emphasis,
        }

        public string localizationKey;
        public Transform bubbleContainer;
        public Image[] bubbleImages;
        public Text textSize;
        public Text text;
        public float speechSpeedInterval = 0.02f;
        public float speechWaitTime = 1.0f;
        public float bubbleTweenTime = 0.2f;

        public float speechBreakTime = 0.0f;
        public float destroyTime = 4.0f;

        private int _speechCount = 0;

        public void Init()
        {
            _speechCount = LocalizationManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public void Clear()
        {
            StopAllCoroutines();
            DOTween.Kill(this);
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

        public void SetBubbleImage(int index)
        {
            for (int i = 0; i < bubbleImages.Length; ++i)
            {
                bubbleImages[i].gameObject.SetActive(index == i);
            }
        }

        public IEnumerator CoShowText()
        {
            if (_speechCount == 0)
                yield break;

            gameObject.SetActive(true);

            string speech = LocalizationManager.Localize($"{localizationKey}{Random.Range(0, _speechCount)}");
            if (!string.IsNullOrEmpty(speech))
            {
                if (speech.StartsWith("!"))
                {
                    speech = speech.Substring(1);
                    SetBubbleImage(1);
                }
                else
                    SetBubbleImage(0);

                textSize.text = speech;
                textSize.rectTransform.DOScale(0.0f, 0.0f);
                textSize.rectTransform.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);
                
                var tweenScale = DOTween.Sequence();
                tweenScale.Append(bubbleContainer.DOScale(1.1f, 1.4f));
                tweenScale.Append(bubbleContainer.DOScale(1.0f, 1.4f));
                tweenScale.SetLoops(3);
                tweenScale.Play();

                var tweenMoveBy = DOTween.Sequence();
                tweenMoveBy.Append(textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, 6.0f), 1.4f));
                tweenMoveBy.Append(textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, -6.0f), 1.4f));
                tweenMoveBy.SetLoops(3);
                tweenMoveBy.Play();

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

            bubbleContainer.DOKill();
            textSize.transform.DOKill();
            gameObject.SetActive(false);
        }

        public void Hide()
        {
            text.text = "";
            gameObject.SetActive(false);
        }
    }
}
