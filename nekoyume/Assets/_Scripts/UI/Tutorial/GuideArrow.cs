using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideArrow : TutorialItem
    {
        [SerializeField] private Material growOutline;
        private RectTransform _rectTransform;
        private Animator _arrow;
        private Coroutine _coroutine;
        private Image _cachedImage;

        private readonly Dictionary<GuideType, int> _guideTypes =
            new Dictionary<GuideType, int>(new GuideTypeEqualityComparer());

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _arrow = GetComponent<Animator>();

            for (int i = 0; i < (int) GuideType.End; ++i)
            {
                var type = (GuideType) i;
                _guideTypes.Add(type, Animator.StringToHash(type.ToString()));
            }
        }

        private IEnumerator PlayAnimation(GuideType guideType,
            bool isSkip,
            System.Action callback)
        {
            _arrow.Play(_guideTypes[guideType], -1, isSkip ? 1 : 0);
            var length = _arrow.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
            callback?.Invoke();
        }

        private void ClearCachedImageMaterial()
        {
            if (_cachedImage != null)
            {
                _cachedImage.material = null;
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

                ClearCachedImageMaterial();
                _rectTransform.anchoredPosition  = d.Target.anchoredPosition;
                _rectTransform.sizeDelta = d.Target.sizeDelta;
                if (d.GuideType == GuideType.Outline)
                {
                    _cachedImage = d.Target.GetComponent<Image>();
                    _cachedImage.material = growOutline;
                }

                _coroutine = StartCoroutine(PlayAnimation(d.GuideType, d.IsSkip, callback));
            }
        }

        public override void Stop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            ClearCachedImageMaterial();
            _coroutine = StartCoroutine(PlayAnimation(GuideType.Stop, false, null));
        }
    }
}
