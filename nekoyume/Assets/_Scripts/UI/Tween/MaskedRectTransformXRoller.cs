using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
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

        private RectTransform _rectTransform = null;

        private Coroutine _coroutine = null;

        private Vector2 _originalPos;

        private float LeftInXPosition => _originalPos.x;

        private float LeftOutXPosition => -content.rectTransform.rect.width;

        private float RightInXPosition => LeftOutXPosition + _rectTransform.rect.width;

        private float RightOutXPosition => _rectTransform.rect.width;

        private float _realAnimationTime;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            KillTween();

            _coroutine = StartCoroutine(CoScrollContent());
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

            yield return null;
            if (_rectTransform.rect.width >= content.rectTransform.rect.width)
            {
                yield break;
            }

            if (curve is null)
            {
                Debug.LogError($"Curve not set.");
                yield break;
            }

            _originalPos = content.rectTransform.anchoredPosition;
            var rightOutX = RightOutXPosition;
            var rightInX = RightInXPosition;
            var leftOutX = LeftOutXPosition;
            var leftInX = LeftInXPosition;
            var elapsedTime = 0f;
            var wasPaused = false;
            var pauseTime = pauseTiming switch
            {
                PauseTiming.Left => (rightInX - leftOutX) / (rightOutX - leftOutX),
                PauseTiming.Middle => ((leftOutX + rightOutX) / 2 - leftOutX) / (rightOutX - leftOutX),
                PauseTiming.Right => (leftInX - leftOutX) / (rightOutX - leftOutX),
                _ => throw new ArgumentOutOfRangeException()
            };

            yield return new WaitForSeconds(startDelay);
            while (gameObject.activeSelf)
            {
                var t = curve.Evaluate(elapsedTime / _realAnimationTime);
                var xPos = Mathf.Lerp(isInfiniteScroll ? rightOutX : leftInX,  isInfiniteScroll ? leftOutX : rightInX, t);
                content.rectTransform.anchoredPosition = new Vector2(xPos, _originalPos.y);

                elapsedTime += Time.deltaTime;
                if (t >= pauseTime && !wasPaused && isInfiniteScroll)
                {
                    yield return new WaitForSeconds(this.pauseTime);
                    wasPaused = true;
                }

                if (elapsedTime > _realAnimationTime)
                {
                    wasPaused = false;
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
    }
}
