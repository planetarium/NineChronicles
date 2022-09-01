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
        public RectTransform contentSize;
        public TextMeshProUGUI tempText;
        public TextMeshProUGUI realText;
        public float speechSpeedInterval = 0.02f;
        public float speechWaitTime = 1.0f;
        public float bubbleTweenTime = 0.2f;

        public float speechBreakTime;
        public float destroyTime = 4.0f;
        public bool enable;

        private int SpeechCount { get; set; }
        private Coroutine _coroutine;
        private Sequence _tweenScale;
        private Sequence _tweenMoveBy;
        private string _defaultKey;
        private bool _forceFixed = false;

        protected override void Awake()
        {
            base.Awake();
            _defaultKey = localizationKey;
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
            _forceFixed = false;
            realText.text = string.Empty;
            KillTween();
            base.OnDisable();
        }

        public void Init(bool active = true)
        {
            enable = active;
            SpeechCount = L10nManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        private void KillTween()
        {
            if (_tweenScale != null)
            {
                _tweenScale.Complete();
                _tweenScale.Kill();
                _tweenScale = null;
            }

            if (_tweenMoveBy != null)
            {
                _tweenMoveBy.Complete();
                _tweenMoveBy.Kill();
                _tweenMoveBy = null;
            }

            if (bubbleContainer != null)
            {
                bubbleContainer.DOKill();
            }

            if (contentSize != null)
            {
                contentSize.DOKill();
            }
        }

        public void UpdatePosition(Camera camera, GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition =
                targetPosition.ToCanvasPosition(camera, MainCanvas.instance.Canvas);
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
            realText.text = string.Empty;
            gameObject.SetActive(false);
        }

        public void Close()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(CoClose());
            }
        }

        private void BeforeSpeech()
        {
            if (!(_coroutine is null))
            {
                StopCoroutine(_coroutine);
            }

            KillTween();
            realText.text = tempText.text = string.Empty;
            gameObject.SetActive(true);
        }

        public void ShowText(bool instant)
        {
            if (!enable || SpeechCount == 0)
            {
                return;
            }

            BeforeSpeech();
            var speech =
                L10nManager.Localize($"{localizationKey}{Random.Range(0, SpeechCount)}");
            _coroutine = StartCoroutine(ShowText(speech, instant, false));
        }

        public IEnumerator CoShowText(bool instant = false)
        {
            if (!enable || SpeechCount == 0)
            {
                yield break;
            }

            ShowText(instant);
            yield return _coroutine;
        }

        public IEnumerator CoShowText(string speech, bool instant = false)
        {
            BeforeSpeech();
            _coroutine = StartCoroutine(ShowText(speech, instant, false));
            yield return _coroutine;
        }

        private IEnumerator ShowText(string speech, bool instant, bool forceFixed)
        {
            _forceFixed = forceFixed;
            tempText.text = string.Empty;
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

                tempText.text = speech;
                contentSize.DOScale(0.0f, 0.0f);
                contentSize.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);

                if (_tweenScale == null ||
                    !_tweenScale.IsActive() ||
                    !_tweenScale.IsPlaying())
                {
                    _tweenScale = DOTween.Sequence();
                    _tweenScale.Append(bubbleContainer.DOScale(1.1f, 1.4f));
                    _tweenScale.Append(bubbleContainer.DOScale(1.0f, 1.4f));
                    _tweenScale.SetLoops(_forceFixed ? -1 : 3);
                    _tweenScale.Play();
                    _tweenScale.onComplete = () => _tweenScale = null;
                }

                if (_tweenMoveBy == null ||
                    !_tweenMoveBy.IsActive() ||
                    !_tweenMoveBy.IsPlaying())
                {
                    _tweenMoveBy = DOTween.Sequence();
                    _tweenMoveBy.Append(
                        contentSize.DOBlendableLocalMoveBy(new Vector3(0.0f, 6.0f), 1.4f));
                    _tweenMoveBy.Append(
                        contentSize.DOBlendableLocalMoveBy(new Vector3(0.0f, -6.0f), 1.4f));
                    _tweenMoveBy.SetLoops(_forceFixed ? -1 : 3);
                    _tweenMoveBy.Play();
                    _tweenMoveBy.onComplete = () => _tweenMoveBy = null;
                }

                yield return new WaitForSeconds(bubbleTweenTime);

                if (instant)
                {
                    realText.text = speech;
                }
                else
                {
                    for (var i = 1; i <= speech.Length; ++i)
                    {
                        realText.text = i == speech.Length
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
                yield return new WaitWhile(() => forceFixed);

                yield return CoClose();
            }

            yield return new WaitForSeconds(breakTime);
            Hide();
        }

        public override IEnumerator CoClose()
        {
            realText.text = string.Empty;
            contentSize.DOScale(0.0f, bubbleTweenTime).SetEase(Ease.InBack);
            yield return new WaitForSeconds(bubbleTweenTime);
        }

        public void ResetKey()
        {
            localizationKey = _defaultKey;
        }
    }
}
