using System;
using Nekoyume.EnumType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PageView : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
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

        private int _currentIndex;

        private Vector2 _initialPosition;

        private bool _isPageMoving;

        private bool _isDragging;

        private Coroutine _coroutine;

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
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
            _coroutine = StartCoroutine(CoMovePage());
        }

        private void UpdateView()
        {
            _content.anchoredPosition = _initialPosition;
            _panelPosition = _content.localPosition;
            _xBorderMax = _panelPosition.x;
            _xBorderMin = _xBorderMax - maskTransform.rect.width * (_content.childCount - 1);
            SetPageIndex(0);
            _isPageMoving = false;
            _isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isPageMoving)
            {
                _isDragging = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }
            var delta = eventData.pressPosition.x - eventData.position.x;
            var x = Mathf.Clamp(_panelPosition.x - delta, _xBorderMin, _xBorderMax);
            _content.localPosition = new Vector3(x, _content.localPosition.y, _content.localPosition.z);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isPageMoving && !_isDragging)
            {
                _isDragging = false;
                return;
            }

            _isDragging = false;
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

                newX = Mathf.Clamp(newX, _xBorderMin, _xBorderMax);
                targetPosition = new Vector3(newX, _content.localPosition.y, _content.localPosition.z);
            }

            var pageDiff = Mathf.RoundToInt((_panelPosition.x - newX) / contentWidth);
            var newIndex = _currentIndex + pageDiff;
            StartCoroutine(CoSmoothMovePage(_content.localPosition, targetPosition, newIndex));
            SetPageIndex(newIndex);
        }

        private IEnumerator CoSmoothMovePage(Vector3 startPos, Vector3 endPos, int index)
        {
            _isPageMoving = true;
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

            SetPageIndex(index);
            _content.localPosition = endPos;
            _panelPosition = _content.localPosition;
            _isPageMoving = false;
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
            yield return new WaitUntil(() => _content);

            var waitInterval = new WaitForSeconds(movePageInterval);
            var contentWidth = maskTransform.rect.width;
            while (gameObject.activeSelf)
            {
                do
                {
                    yield return waitInterval;
                } while (_isDragging);

                var idx = _currentIndex + 1 < _indexImages.Count ?
                    _currentIndex + 1 : 0;
                var x = _xBorderMax - (idx * contentWidth);
                var targetPosition = new Vector3(x, _content.localPosition.y, _content.localPosition.z);

                StartCoroutine(CoSmoothMovePage(_content.localPosition, targetPosition, idx));
            }
        }
    }
}
