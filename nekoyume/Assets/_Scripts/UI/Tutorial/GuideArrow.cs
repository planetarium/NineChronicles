using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideArrow : TutorialItem
    {
        [SerializeField] private Material growOutline;
        [SerializeField] private Material spriteDefault;
        private RectTransform _rectTransform;
        private Animator _arrow;
        private Coroutine _coroutine;
        private Image _cachedImage;
        private Menu _menu;

        private readonly Dictionary<GuideType, int> _guideTypes =
            new Dictionary<GuideType, int>(new GuideTypeEqualityComparer());

        private void Awake()
        {
            _menu = Widget.Find<Menu>();
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
                    Vector3 position = d.target.position;
                    position = new Vector3(position.x + d.targetPositionOffset.x,
                        position.y + d.targetPositionOffset.y, position.z);
                    _rectTransform.position = position;

                    Vector2 sizeDelta = d.target.sizeDelta + d.targetSizeOffset;
                    _rectTransform.sizeDelta = sizeDelta;

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


        private void ApplyOutline(RectTransform target)
        {
            _cachedImage = target.GetComponent<Image>();
            if (_cachedImage != null)
            {
                _cachedImage.material = growOutline;
            }

            var menu = target.GetComponent<MainMenu>();
            if (menu != null)
            {
                switch (menu.type)
                {
                    case MenuType.Combination:
                        _menu.combinationSpriteRenderer.material = growOutline;
                        break;
                    case MenuType.Quest:
                        _menu.hasSpriteRenderer.material = growOutline;
                        break;
                }
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

            _menu.combinationSpriteRenderer.material = spriteDefault;
            _menu.hasSpriteRenderer.material = spriteDefault;
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
