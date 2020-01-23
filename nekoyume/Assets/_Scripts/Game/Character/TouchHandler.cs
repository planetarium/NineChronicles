using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.Game.Character
{
    public class TouchHandler : MonoBehaviour, IPointerClickHandler
    {
        public readonly Subject<PointerEventData> OnClick = new Subject<PointerEventData>();
        public readonly Subject<PointerEventData> OnDoubleClick = new Subject<PointerEventData>();
        public readonly Subject<PointerEventData> OnMultipleClick = new Subject<PointerEventData>();
        public readonly Subject<PointerEventData> OnMiddleClick = new Subject<PointerEventData>();
        public readonly Subject<PointerEventData> OnRightClick = new Subject<PointerEventData>();

        public PointerEventData PointerEventData { get; private set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            PointerEventData = eventData;

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    switch (PointerEventData.clickCount)
                    {
                        case 1:
                            OnClick.OnNext(eventData);
                            break;
                        case 2:
                            OnDoubleClick.OnNext(eventData);
                            break;
                        default:
                            OnMultipleClick.OnNext(eventData);
                            break;
                    }

                    break;
                case PointerEventData.InputButton.Right:
                    OnRightClick.OnNext(eventData);
                    break;
                case PointerEventData.InputButton.Middle:
                    OnMiddleClick.OnNext(eventData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetCollider(BoxCollider boxCollider, Vector3 localPosition, Vector3 localScale)
        {
            var size = boxCollider.size;
            var center = boxCollider.center;
            var collider2D = GetComponent<BoxCollider2D>();
            
            if (!(collider2D is null))
            {    
                collider2D.offset = new Vector2(
                    center.x * localScale.x + localPosition.x,
                    center.y * localScale.y + localPosition.y
                    );
                collider2D.size = new Vector2(
                    size.x * localScale.x,
                    size.y * localScale.y);
                
                return;
            }
            
            var collider = GetComponent<BoxCollider>();

            if (!(collider is null))
            {
                collider.center = new Vector3(
                    center.x * localScale.x + localPosition.x,
                    center.y * localScale.y + localPosition.y,
                    center.z * localScale.z + localPosition.z
                    );
                collider.size = new Vector3(
                    size.x * localScale.x, 
                    size.y * localScale.y,
                    size.z * localScale.z);
                
                return;
            }
        }
    }
}
