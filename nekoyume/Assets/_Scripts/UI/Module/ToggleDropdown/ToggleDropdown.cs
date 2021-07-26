using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;

namespace Nekoyume.UI.Module
{
    [Serializable]
    public class ToggleDropdown : Toggle
    {
        public List<Toggle> items = new List<Toggle>();
        public float duration;
        public bool allOffOnAwake = false;

        private List<RectTransform> _itemRectTransforms = new List<RectTransform>();
        private List<CanvasGroup> _itemCanvasGroups = new List<CanvasGroup>();
        private Sequence _seq;
        private RectTransform _rectTransform;
        private Vector2 _parentSize;

        protected override void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentSize = _rectTransform.sizeDelta;
            if (items.Count > 0)
            {
                _itemRectTransforms.AddRange(items.Select(x => x.GetComponent<RectTransform>()));
                _itemCanvasGroups.AddRange(items.Select(x => x.GetComponent<CanvasGroup>()));

                items.First().isOn = false;
                if (!allOffOnAwake)
                {
                    items.First().isOn = true;
                }
            }

            base.Awake();
        }

        protected override void UpdateObject(bool value)
        {
            base.UpdateObject(value);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            foreach (var item in _itemRectTransforms)
            {
                item.anchoredPosition = Vector3.zero;
            }

            foreach (var canvasGroup in _itemCanvasGroups)
            {
                canvasGroup.alpha = 0;
            }

            _seq?.Kill();

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                _parentSize = _rectTransform.sizeDelta;
            }

            _rectTransform.sizeDelta = _parentSize;

            if (value)
            {
                _seq = DOTween.Sequence();

                var eachDuration = duration / _itemRectTransforms.Count;
                for (int i = 0; i < _itemRectTransforms.Count; i++)
                {
                    var item = _itemRectTransforms[i];
                    var itemCanvasGroup = _itemCanvasGroups[i];
                    var itemSizeDelta = item.sizeDelta;

                    var targetMoveY = (i) * -itemSizeDelta.y - _parentSize.y;
                    var targetSize = new Vector2(_parentSize.x,
                        (i + 1) * itemSizeDelta.y + _parentSize.y);
                    _seq.Append(item.DoAnchoredMoveY(targetMoveY, eachDuration))
                        .Join(_rectTransform.DOSizeDelta(targetSize, eachDuration))
                        .Join(itemCanvasGroup.DOFade(1, eachDuration).SetEase(Ease.InExpo));
                }

                _seq.Play();
            }
        }
    }
}
