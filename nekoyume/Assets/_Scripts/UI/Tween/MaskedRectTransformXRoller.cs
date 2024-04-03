using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    using UniRx;
    [RequireComponent(typeof(Mask))]
    public class MaskedRectTransformXRoller : MonoBehaviour
    {
        private enum PauseTiming
        {
            Left,
            Middle,
            Right
        }

        [SerializeField]
        private bool isInfiniteScroll = false;

        [SerializeField]
        private TMP_Text content = null;

        [SerializeField]
        private AnimationCurve curve = null;

        [SerializeField]
        private float startDelay = 1.5f;

        [SerializeField]
        private float endDelay = 1.5f;

        [SerializeField]
        private float targetAnimationTime = 2f;

        [SerializeField]
        private bool useAnimationSpeed;

        [SerializeField] [Range(0f, 1f)]
        private float animationSpeed;

        [SerializeField]
        private PauseTiming pauseTiming;

        [SerializeField]
        private float pauseTime;

        [SerializeField]
        private bool scrollOnlyWhenNeed;

        private RectTransform _rectTransform = null;

        private Coroutine _coroutine = null;

        private Vector2 _originalPos;

        private float LeftInXPosition => _originalPos.x;

        private float LeftOutXPosition => -content.rectTransform.rect.width;

        private float RightInXPosition => LeftOutXPosition + _rectTransform.rect.width;

        private float RightOutXPosition => _rectTransform.rect.width;

        private float _realAnimationTime;

        private bool _isEnabledAndFirstLoop;

        public BoolReactiveProperty isSelected = new BoolReactiveProperty(true);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            isSelected.Subscribe(b =>
            {
                if (b && _coroutine is null)
                {
                    OnEnable();
                }
                else if (!b && !(_coroutine is null))
                {
                    KillTween();
                }
            });
        }

        private void OnEnable()
        {
            KillTween();

            if (scrollOnlyWhenNeed)
            {
                if (_rectTransform.rect.x <= content.rectTransform.rect.x)
                {
                    _coroutine = StartCoroutine(CoScrollContent());
                }
            }
            else
            {
                _coroutine = StartCoroutine(CoScrollContent());
            }
        }

        private void OnDisable()
        {
            KillTween();
        }

        public void SetText(string str)
        {
            content.text = str;
            OnEnable();
        }

        private void KillTween()
        {
            if (_coroutine is null)
            {
                return;
            }

            StopCoroutine(_coroutine);
            content.rectTransform.anchoredPosition = _originalPos;
            _coroutine = null;
        }

        private IEnumerator CoScrollContent()
        {
            _realAnimationTime = useAnimationSpeed
                ? animationSpeed * content.text.Length
                : targetAnimationTime;

            _isEnabledAndFirstLoop = true;

            yield return null;
            if (_rectTransform.rect.width >= content.rectTransform.rect.width)
            {
                yield break;
            }

            if (curve is null)
            {
                NcDebug.LogError($"Curve not set.");
                yield break;
            }

            _originalPos = content.rectTransform.anchoredPosition;
            var rightOutX = RightOutXPosition;
            var rightInX = RightInXPosition;
            var leftOutX = LeftOutXPosition;
            var leftInX = LeftInXPosition;
            var elapsedTime = 0f;
            var wasPaused = false;

            yield return new WaitForSeconds(startDelay);
            while (gameObject.activeSelf)
            {
                var t = curve.Evaluate(elapsedTime / _realAnimationTime);
                var xStart = _isEnabledAndFirstLoop ? leftInX : isInfiniteScroll ? rightOutX : leftInX;
                var xEnd = _isEnabledAndFirstLoop && isInfiniteScroll ? leftOutX : isInfiniteScroll ? leftOutX : rightInX;
                var xPos = Mathf.Lerp(xStart, xEnd, t);
                content.rectTransform.anchoredPosition = new Vector2(xPos, _originalPos.y);

                elapsedTime += Time.deltaTime;
                if (!wasPaused && IsShouldPause(!isInfiniteScroll || _isEnabledAndFirstLoop, pauseTiming, t))
                {
                    yield return new WaitForSeconds(pauseTime);
                    wasPaused = true;
                }

                if (elapsedTime > _realAnimationTime)
                {
                    wasPaused = false;
                    _isEnabledAndFirstLoop = false;
                    if (isInfiniteScroll)
                    {
                        content.rectTransform.anchoredPosition = new Vector2(rightOutX, _originalPos.y);
                        elapsedTime = 0f;
                        yield return new WaitForSeconds(startDelay);
                    }
                    else
                    {
                        yield return new WaitForSeconds(endDelay);
                        content.rectTransform.anchoredPosition = new Vector2(leftInX, _originalPos.y);
                        elapsedTime = 0f;
                        yield return new WaitForSeconds(startDelay);
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        private bool IsShouldPause(bool startFromLeft, PauseTiming timing, float t)
        {
            if (startFromLeft)
            {
                var contentRectTransform = content.rectTransform;
                return timing switch
                {
                    PauseTiming.Left => true,
                    PauseTiming.Middle => contentRectTransform.anchoredPosition.x +
                        contentRectTransform.rect.width / 2 < (LeftInXPosition + RightOutXPosition) / 2,
                    PauseTiming.Right => contentRectTransform.anchoredPosition.x +
                        contentRectTransform.rect.width < RightInXPosition,
                    _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
                };

            }

            return t >= timing switch
            {
                PauseTiming.Left => (RightInXPosition - LeftOutXPosition) /
                                    (RightOutXPosition - LeftOutXPosition),
                PauseTiming.Middle => .5f,
                PauseTiming.Right => (LeftInXPosition - LeftOutXPosition) /
                                     (RightOutXPosition - LeftOutXPosition),
                _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
            };
        }
    }
}
