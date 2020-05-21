using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.Game.Character
{
    public class TouchHandler : MonoBehaviour, IPointerClickHandler
    {
        private readonly Subject<PointerEventData> _onClick = new Subject<PointerEventData>();
        private readonly Subject<PointerEventData> _onDoubleClick = new Subject<PointerEventData>();
        private readonly Subject<PointerEventData> _onMultipleClick = new Subject<PointerEventData>();
        private readonly Subject<PointerEventData> _onMiddleClick = new Subject<PointerEventData>();
        private readonly Subject<PointerEventData> _onRightClick = new Subject<PointerEventData>();

        public IObservable<PointerEventData> OnClick => _onClick;
        public IObservable<PointerEventData> OnDoubleClick => _onDoubleClick;
        public IObservable<PointerEventData> OnMultipleClick => _onMultipleClick;
        public IObservable<PointerEventData> OnMiddleClick => _onMiddleClick;
        public IObservable<PointerEventData> OnRightClick => _onRightClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    switch (eventData.clickCount)
                    {
                        case 1:
                            _onClick.OnNext(eventData);
                            break;
                        case 2:
                            _onDoubleClick.OnNext(eventData);
                            break;
                        default:
                            _onMultipleClick.OnNext(eventData);
                            break;
                    }

                    break;
                case PointerEventData.InputButton.Middle:
                    _onMiddleClick.OnNext(eventData);
                    break;
                case PointerEventData.InputButton.Right:
                    _onRightClick.OnNext(eventData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetCollider(BoxCollider boxCollider, Vector3 localPosition, Vector3 localScale)
        {
            var size = boxCollider.size;
            var center = boxCollider.center;
            var col2D = GetComponent<BoxCollider2D>();
            if (col2D)
            {
                col2D.offset = new Vector2(
                    center.x * localScale.x + localPosition.x,
                    center.y * localScale.y + localPosition.y
                    );
                col2D.size = new Vector2(
                    size.x * localScale.x,
                    size.y * localScale.y);

                return;
            }

            var col = GetComponent<BoxCollider>();
            if (!col)
            {
                return;
            }

            col.center = new Vector3(
                center.x * localScale.x + localPosition.x,
                center.y * localScale.y + localPosition.y,
                center.z * localScale.z + localPosition.z
            );
            col.size = new Vector3(
                size.x * localScale.x,
                size.y * localScale.y,
                size.z * localScale.z);
        }
    }
}
