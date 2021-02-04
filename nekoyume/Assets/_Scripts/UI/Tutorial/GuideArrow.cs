using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
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
                    _rectTransform.position = d.target.position;
                    _rectTransform.sizeDelta = d.target.sizeDelta;
                    if (d.guideType == GuideType.Outline)
                    {
                        _cachedImage = d.target.GetComponent<Image>();
                        _cachedImage.material = growOutline;
                    }
                }

                AudioController.instance.PlaySfx(AudioController.SfxCode.Notice);
                _coroutine = StartCoroutine(PlayAnimation(d.guideType, d.isSkip, callback));
            }
        }

        public override void Stop(System.Action callback)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            Reset();
            _coroutine = StartCoroutine(PlayAnimation(GuideType.Stop, false, callback));
        }

        private IEnumerator PlayAnimation(GuideType guideType,
            bool isSkip,
            System.Action callback)
        {
            yield return new WaitForSeconds(predelay);
            _arrow.Play(_guideTypes[guideType], -1, isSkip ? 1 : 0);
            var length = _arrow.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
            callback?.Invoke();
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
    }
}
