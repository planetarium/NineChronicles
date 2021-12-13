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

        private readonly Dictionary<GuideType, int> _guideTypes =
            new Dictionary<GuideType, int>(new GuideTypeEqualityComparer());

        private readonly List<Vector2> _arrowDefaultPositionOffset = new List<Vector2>();

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _arrow = GetComponent<Animator>();

            for (var i = 0; i < (int) GuideType.End; ++i)
            {
                var type = (GuideType) i;
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
                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                }

                Reset();
                _arrow.Play(_guideTypes[GuideType.Stop]);

                if (d.target == null)
                {
                    d.guideType = GuideType.Stop;
                }

                if (d.guideType != GuideType.Stop)
                {
                    for (var i = 0; i < arrowTransform.Count; i++)
                    {
                        arrowTransform[i].anchoredPosition =
                            _arrowDefaultPositionOffset[i] + d.arrowPositionOffset;
                    }
                    _rectTransform.anchoredPosition = d.target.anchoredPosition;
                    _rectTransform.anchorMin = d.target.anchorMin;
                    _rectTransform.anchorMax = d.target.anchorMax;
                    _rectTransform.pivot = d.target.pivot;
                    _rectTransform.position = d.target.position;
                    _rectTransform.anchoredPosition += d.targetPositionOffset;
                    _rectTransform.sizeDelta = d.target.sizeDelta + d.targetSizeOffset;
                    if (d.guideType == GuideType.Outline)
                    {
                        ApplyOutline(d.target);
                    }
                }

                _coroutine = StartCoroutine(PlayAnimation(d.guideType, d.arrowAdditionalDelay, d.isSkip, callback));
            }
        }

        public override void Stop(System.Action callback)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            Reset();
            _coroutine = StartCoroutine(PlayAnimation(GuideType.Stop, 0,false, callback));
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
    }
}
