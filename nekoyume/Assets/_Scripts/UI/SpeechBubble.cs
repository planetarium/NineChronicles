using DG.Tweening;
using Nekoyume.Game;
using System.Collections;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class SpeechBubble : HudWidget
    {
        public string localizationKey;
        public Transform bubbleContainer;
        public Image[] bubbleImages;
        public TextMeshProUGUI textSize;
        public TextMeshProUGUI text;
        public float speechSpeedInterval = 0.02f;
        public float speechWaitTime = 1.0f;
        public float bubbleTweenTime = 0.2f;

        public float speechBreakTime;
        public float destroyTime = 4.0f;
        public bool enable;
        public bool onGoing;

        private int SpeechCount { get; set; }
        private Coroutine _coroutine;
        private Sequence _tweenScale;
        private Sequence _tweenMoveBy;
        private string _defaultKey;

        protected override void Awake()
        {
            base.Awake();
            _defaultKey = localizationKey;
        }

        protected override void OnDisable()
        {
            _tweenScale?.Kill();
            _tweenScale = null;
            _tweenMoveBy?.Kill();
            _tweenMoveBy = null;
            base.OnDisable();
        }

        public void Init(bool active = true)
        {
            enable = active;
            SpeechCount = L10nManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition =
                targetPosition.ToCanvasPosition(
                    ActionCamera.instance.Cam,
                    MainCanvas.instance.Canvas);
        }

        public bool SetKey(string value)
        {
            localizationKey = value;
            SpeechCount = L10nManager.LocalizedCount(localizationKey);
            return SpeechCount > 0;
        }

        private void SetBubbleImage(int index)
        {
            for (var i = 0; i < bubbleImages.Length; ++i)
            {
                bubbleImages[i].gameObject.SetActive(index == i);
            }
            SetBubbleImageInternal();
        }

        protected virtual void SetBubbleImageInternal()
        {
        }

        public virtual void Hide()
        {
            text.text = "";
            gameObject.SetActive(false);
        }

        private void BeforeSpeech()
        {
            if (!(_coroutine is null))
            {
                StopCoroutine(_coroutine);
            }

            gameObject.SetActive(true);
        }

        public IEnumerator CoShowText(bool instant = false)
        {
            if (!enable || SpeechCount == 0)
            {
                yield break;
            }

            BeforeSpeech();
            var speech =
                L10nManager.Localize($"{localizationKey}{Random.Range(0, SpeechCount)}");
            _coroutine = StartCoroutine(ShowText(speech, instant));
            yield return _coroutine;
        }

        public IEnumerator CoShowText(string speech, bool instant = false)
        {
            if (!enable || SpeechCount == 0)
            {
                yield break;
            }

            BeforeSpeech();
            _coroutine = StartCoroutine(ShowText(speech, instant));
            yield return _coroutine;
        }

        private IEnumerator ShowText(string speech, bool instant = false)
        {
            text.text = "";
            var breakTime = speechBreakTime;
            if (!string.IsNullOrEmpty(speech))
            {
                if (speech.StartsWith("!"))
                {
                    breakTime /= 2;
                    speech = speech.Substring(1);
                    SetBubbleImage(1);
                }
                else
                    SetBubbleImage(0);

                textSize.text = speech;
                textSize.rectTransform.DOScale(0.0f, 0.0f);
                textSize.rectTransform.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);

                if (_tweenScale is null ||
                    !_tweenScale.IsActive() ||
                    !_tweenScale.IsPlaying())
                {
                    _tweenScale = DOTween.Sequence();
                    _tweenScale.Append(bubbleContainer.DOScale(1.1f, 1.4f));
                    _tweenScale.Append(bubbleContainer.DOScale(1.0f, 1.4f));
                    _tweenScale.SetLoops(3);
                    _tweenScale.Play();
                    _tweenScale.onComplete = () => _tweenScale = null;
                }

                if (_tweenMoveBy is null ||
                    !_tweenMoveBy.IsActive() ||
                    !_tweenMoveBy.IsPlaying())
                {
                    _tweenMoveBy = DOTween.Sequence();
                    _tweenMoveBy.Append(
                        textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, 6.0f), 1.4f));
                    _tweenMoveBy.Append(
                        textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, -6.0f), 1.4f));
                    _tweenMoveBy.SetLoops(3);
                    _tweenMoveBy.Play();
                    _tweenMoveBy.onComplete = () => _tweenMoveBy = null;
                }

                yield return new WaitForSeconds(bubbleTweenTime);

                if (instant)
                {
                    text.text = speech;
                }
                else
                {
                    for (var i = 1; i <= speech.Length; ++i)
                    {
                        text.text = i == speech.Length
                            ? $"{speech.Substring(0, i)}"
                            : $"{speech.Substring(0, i)}<alpha=#00>{speech.Substring(i)}";
                        yield return new WaitForSeconds(speechSpeedInterval);

                        // check destroy
                        if (!gameObject)
                        {
                            break;
                        }
                    }
                }

                yield return new WaitForSeconds(speechWaitTime);
                yield return new WaitWhile(() => onGoing);

                text.text = "";
                textSize.rectTransform.DOScale(0.0f, bubbleTweenTime).SetEase(Ease.InBack);
                yield return new WaitForSeconds(bubbleTweenTime);
            }

            yield return new WaitForSeconds(breakTime);

            bubbleContainer.DOKill();
            textSize.transform.DOKill();
            Hide();
        }

        public void ResetKey()
        {
            localizationKey = _defaultKey;
        }
    }
}
