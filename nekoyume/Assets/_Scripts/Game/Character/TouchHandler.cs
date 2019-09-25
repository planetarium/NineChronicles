using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.Game.Character
{
    public class TouchHandler : MonoBehaviour, IPointerClickHandler
    {
        public readonly Subject<TouchHandler> onPointerClick = new Subject<TouchHandler>();
        
        public PointerEventData PointerEventData { get; private set; }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            PointerEventData = eventData;
            onPointerClick.OnNext(this);
        }
    }
}
