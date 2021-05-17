using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    public class MaskedRectTransformXRoller : MonoBehaviour
    {
        [SerializeField]
        private bool isInfiniteScroll = false;

        [SerializeField]
        private RectTransform contentRect = null;

        [SerializeField]
        private float targetAnimationTime = 2f;

        [SerializeField]
        private AnimationCurve curve = null;

        [SerializeField]
        private float startDelay = 1.5f;

        [SerializeField]
        private float endDelay = 1.5f;

        private RectTransform _rectTransform = null;

        private Coroutine _coroutine = null;

        private Vector2 _originalPos;

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

        private void KillTween()
        {
            if (_coroutine is null)
            {
                return;
            }

            StopCoroutine(_coroutine);
            contentRect.anchoredPosition = _originalPos;
            _coroutine = null;
        }

        private IEnumerator CoScrollContent()
        {
            yield return null;
            if (_rectTransform.rect.width >= contentRect.rect.width)
            {
                yield break;
            }

            if (curve is null)
            {
                Debug.LogError($"Curve not set.");
                yield break;
            }

            _originalPos = contentRect.anchoredPosition;

            var xDiff = contentRect.rect.width - _rectTransform.rect.width;
            var targetX = isInfiniteScroll ?
                _originalPos.x - xDiff - _rectTransform.rect.width :
                _originalPos.x - xDiff;

            var elapsedTime = 0f;

            yield return new WaitForSeconds(startDelay);
            while (gameObject.activeSelf)
            {
                var t = curve.Evaluate(elapsedTime / targetAnimationTime);
                var xPos = Mathf.Lerp(
                    isInfiniteScroll ?
                    _originalPos.x + _rectTransform.rect.width :
                    _originalPos.x,
                    targetX,
                    t);
                contentRect.anchoredPosition = new Vector2(xPos, _originalPos.y);

                elapsedTime += Time.deltaTime;
                if (elapsedTime > targetAnimationTime)
                {
                    if (isInfiniteScroll)
                    {
                        contentRect.anchoredPosition =
                            new Vector2(_originalPos.x + _rectTransform.rect.width, _originalPos.y);
                        elapsedTime = 0f;
                        yield return new WaitForSeconds(startDelay);
                    }
                    else
                    {
                        contentRect.anchoredPosition = new Vector2(targetX, _originalPos.y);
                        yield return new WaitForSeconds(endDelay);
                        contentRect.anchoredPosition = new Vector2(_originalPos.x, _originalPos.y);
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
