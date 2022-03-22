using System;
using Nekoyume.EnumType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PageView : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private RectTransform maskTransform = null;

        [SerializeField]
        private Sprite indexEnabledImage = null;

        [SerializeField]
        private Sprite indexDisabledImage = null;

        [SerializeField, Range(0f, 1.0f)]
        private float dragPercentThreshold;

        [SerializeField]
        private float animationTime;

        [SerializeField]
        private float movePageInterval;

        private RectTransform _content;

        private readonly List<Image> _indexImages = new List<Image>();

        private Vector2 _panelPosition;

        private float _xBorderMin;

        private float _xBorderMax;

        private Coroutine _animationCoroutine = null;

        private int _currentIndex;

        private Vector2 _initialPosition;

        public void Set(RectTransform content, IEnumerable<Image> indexImages)
        {
            _content = content;
            _indexImages.Clear();
            _indexImages.AddRange(indexImages);
            _initialPosition = _content.GetAnchoredPositionOfPivot(PivotPresetType.TopLeft);
            UpdateView();
        }

        private void OnEnable()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            _content.anchoredPosition = _initialPosition;
            _panelPosition = _content.localPosition;
            _xBorderMax = _panelPosition.x;
            _xBorderMin = _xBorderMax - maskTransform.rect.width * (_content.childCount - 1);
            SetPageIndex(0);
            StartCoroutine(CoMovePage());
        }

        public void OnDrag(PointerEventData eventData)
        {
            var delta = eventData.pressPosition.x - eventData.position.x;
            var x = Mathf.Clamp(_panelPosition.x - delta, _xBorderMin, _xBorderMax);
            _content.localPosition = new Vector3(x, _content.localPosition.y, _content.localPosition.z);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var contentWidth = maskTransform.rect.width;
            var percentage = (eventData.pressPosition.x - eventData.position.x) / contentWidth;
            var newX = _panelPosition.x;
            var pageDelta = Mathf.Abs(percentage);
            var targetPosition = _panelPosition;

            if (pageDelta >= dragPercentThreshold)
            {
                var isNextPage = percentage > 0;
                while (pageDelta >= dragPercentThreshold)
                {
                    var direction = isNextPage ? -1 : 1;
                    var delta = direction * contentWidth;

                    newX += delta;
                    --pageDelta;
                }

                var x = Mathf.Clamp(newX, _xBorderMin, _xBorderMax);
                targetPosition = new Vector3(x, _content.localPosition.y, _content.localPosition.z);
                var pageDiff = Mathf.RoundToInt((_panelPosition.x - x) / contentWidth);
                SetPageIndex(_currentIndex + pageDiff);
            }

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(CoSmoothMovePage(_content.localPosition, targetPosition));
        }

        private IEnumerator CoSmoothMovePage(Vector3 startPos, Vector3 endPos)
        {
            var elapsed = .0f;

            while (elapsed < animationTime)
            {
                var t = elapsed / animationTime;
                var step = Mathf.SmoothStep(0f, 1f, t);
                var position = Vector3.Lerp(startPos, endPos, step);

                _content.localPosition = position;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _content.localPosition = endPos;
            _panelPosition = _content.localPosition;
            _animationCoroutine = null;
        }

        private void SetPageIndex(int index)
        {
            _currentIndex = index;

            for (int i = 0; i < _indexImages.Count; ++i)
            {
                var enabled = i == index;
                _indexImages[i].sprite = enabled ? indexEnabledImage : indexDisabledImage;
            }
        }

        private IEnumerator CoMovePage()
        {
            var waitInterval = new WaitForSeconds(movePageInterval);
            var contentWidth = maskTransform.rect.width;
            while (gameObject.activeSelf)
            {
                yield return waitInterval;

                if (_animationCoroutine == null)
                {
                    var idx = _currentIndex + 1 < _indexImages.Count ?
                        _currentIndex + 1 : 0;
                    var x = _xBorderMax - (idx * contentWidth);
                    var targetPosition = new Vector3(x, _content.localPosition.y, _content.localPosition.z);

                    StartCoroutine(CoSmoothMovePage(_content.localPosition, targetPosition));
                    SetPageIndex(idx);
                }
            }
        }
    }
}
