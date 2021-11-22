using System.Collections;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public class MaskedRectTransformXRoller : MonoBehaviour
    {
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

            yield return new WaitForSeconds(startDelay);
            while (gameObject.activeSelf)
            {
                var t = curve.Evaluate(elapsedTime / _realAnimationTime);
                var xPos = Mathf.Lerp(isInfiniteScroll ? rightOutX : leftInX,  isInfiniteScroll ? leftOutX : rightInX, t);
                content.rectTransform.anchoredPosition = new Vector2(xPos, _originalPos.y);

                elapsedTime += Time.deltaTime;
                if (elapsedTime > _realAnimationTime)
                {
                    if (isInfiniteScroll)
                    {
                        content.rectTransform.anchoredPosition = new Vector2(rightOutX, _originalPos.y);
                        elapsedTime = 0f;
                        yield return new WaitForSeconds(startDelay);
                    }
                    else
                    {
                        yield return new WaitForSeconds(endDelay);
                        //content.rectTransform.anchoredPosition = new Vector2(leftOutX, _originalPos.y);
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
