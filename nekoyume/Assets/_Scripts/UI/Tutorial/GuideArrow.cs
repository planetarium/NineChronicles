using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideArrow : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Animator _arrow;
        private Coroutine _coroutine;

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
            Vector2 position,
            bool isSkip,
            System.Action callback)
        {
            _rectTransform.anchoredPosition  = position;
            _arrow.Play(_guideTypes[guideType], -1, isSkip ? 1 : 0);
            var length = _arrow.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
            callback?.Invoke();
        }

        public void Play(GuideArrowData data)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            _coroutine = StartCoroutine(PlayAnimation(data.GuideType, data.Target, data.IsSkip, data.Callback));
        }

        public void Stop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            _coroutine = StartCoroutine(PlayAnimation(GuideType.Stop, Vector2.zero, false, null));
        }
    }
}
