using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.Game.Character
{
    public class TouchHandler : MonoBehaviour, IPointerClickHandler
    {
        public readonly Subject<TouchHandler> OnClick = new Subject<TouchHandler>();
        public readonly Subject<TouchHandler> OnDoubleClick = new Subject<TouchHandler>();
        public readonly Subject<TouchHandler> OnMultipleClick = new Subject<TouchHandler>();
        public readonly Subject<TouchHandler> OnMiddleClick = new Subject<TouchHandler>();
        public readonly Subject<TouchHandler> OnRightClick = new Subject<TouchHandler>();
        
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
                            OnClick.OnNext(this);
                            break;
                        case 2:
                            OnDoubleClick.OnNext(this);
                            break;
                        default:
                            OnMultipleClick.OnNext(this);
                            break;
                    }
                    break;
                case PointerEventData.InputButton.Right:
                    OnRightClick.OnNext(this);
                    break;
                case PointerEventData.InputButton.Middle:
                    OnMiddleClick.OnNext(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
