using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideArrow : TutorialItem
    {
        [SerializeField]
        private Material growOutline;

        [SerializeField]
        private List<RectTransform> arrowTransform;

        private RectTransform _rectTransform;
        private Animator _arrow;
        private Coroutine _coroutine;
        private Image _cachedImage;
        private Camera _camera;

        private readonly Dictionary<GuideType, int> _guideTypes = new(new GuideTypeEqualityComparer());

        private readonly List<Vector2> _arrowDefaultPositionOffset = new();

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _arrow = GetComponent<Animator>();
            _camera = Camera.main;

            for (var i = 0; i < (int)GuideType.End; ++i)
            {
                var type = (GuideType)i;
                _guideTypes.Add(type, Animator.StringToHash(type.ToString()));
            }

            foreach (var arrow in arrowTransform)
            {
                _arrowDefaultPositionOffset.Add(arrow.anchoredPosition);
            }
        }

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideArrowData d)
            {
                if (!gameObject.activeSelf)
                {
                    callback?.Invoke();
                    return;
                }

                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                }

                Reset();

                if (d.target == null)
                {
                    d.guideType = GuideType.Stop;
                }

                _arrow.Play(_guideTypes[GuideType.Stop]);
                _coroutine = StartCoroutine(PlayGuideAnimation(d.guideType, d.arrowAdditionalDelay,
                    d.isSkip,
                    () =>
                    {
                        if (d.guideType != GuideType.Stop)
                        {
                            SetGuideRect(d);
                        }
                    }, callback));
            }
        }

        public override void Stop(System.Action callback)
        {
            if (!gameObject.activeSelf)
            {
                callback?.Invoke();
                return;
            }

            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            Reset();
            _coroutine = StartCoroutine(PlayAnimation(GuideType.Stop, 0, false, callback));
        }

        private IEnumerator PlayGuideAnimation(GuideType guideType, float additionalDelay, bool isSkip, System.Action delayCallback, System.Action callback)
        {
            yield return new WaitForSeconds(predelay + additionalDelay);
            delayCallback?.Invoke();
            _arrow.Play(_guideTypes[guideType], -1, isSkip ? 1 : 0);
            var length = _arrow.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
            callback?.Invoke();
        }

        private IEnumerator PlayAnimation(GuideType guideType, float additionalDelay, bool isSkip, System.Action callback)
        {
            yield return new WaitForSeconds(predelay + additionalDelay);
            _arrow.Play(_guideTypes[guideType], -1, isSkip ? 1 : 0);
            var length = _arrow.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
            callback?.Invoke();
        }


        private void ApplyOutline(Component target)
        {
            _cachedImage = target.GetComponent<Image>();
            if (_cachedImage != null)
            {
                _cachedImage.material = growOutline;
            }
        }

        private void Reset()
        {
            _rectTransform.position = Vector2.zero;
            _rectTransform.sizeDelta = Vector2.zero;
            if (_cachedImage != null)
            {
                _cachedImage.material = null;
            }
        }

        public void PlaySfx()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.GuideArrow);
        }

        public override void Skip(System.Action callback)
        {
            _arrow.Play(_arrow.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 1);
            callback?.Invoke();
        }

        private void SetGuideRect(GuideArrowData data)
        {
            var targetWorldCorners = new Vector3[4];
            var thisWorldCorners = new Vector3[4];

            data.target.GetWorldCorners(targetWorldCorners);

            for (var i = 0; i < 4; i++)
            {
                // 타겟의 각 모서리 월드 좌표를 화면 좌표로 변환
                // Convert A.worldCorners to screen position
                Vector2 screenPos = _camera.WorldToScreenPoint(targetWorldCorners[i]);

                // 변환된 화면 좌표를 B의 월드 좌표로 다시 변환
                // Convert screenPos to world position about _rectTransform
                RectTransformUtility.ScreenPointToWorldPointInRectangle(_rectTransform,
                    screenPos, _camera, out thisWorldCorners[i]);
            }

            Vector2 newCenter = (thisWorldCorners[0] + thisWorldCorners[2]) * 0.5f;
            _rectTransform.position = newCenter;

            _rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                data.target.rect.width);
            _rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                data.target.rect.height);

            _rectTransform.anchoredPosition += data.targetPositionOffset;
            _rectTransform.sizeDelta += data.targetSizeOffset;
            var zeroZPosition = _rectTransform.anchoredPosition3D;
            zeroZPosition.z = 0;
            _rectTransform.anchoredPosition3D = zeroZPosition;

            var arrowCount = arrowTransform.Count;
            for (var i = 0; i < arrowCount; i++)
            {
                arrowTransform[i].anchoredPosition =
                    _arrowDefaultPositionOffset[i] + data.arrowPositionOffset;
            }

            if (data.guideType == GuideType.Outline)
            {
                ApplyOutline(data.target);
            }
        }
    }
}
