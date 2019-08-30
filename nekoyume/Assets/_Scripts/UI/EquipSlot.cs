using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour
    {
        public RectTransform rectTransform;
        public Image gradeImage;
        public Image defaultImage;
        public Image itemImage;
        public ItemUsable item;
        public ItemBase.ItemType type;

        private EventTrigger _eventTrigger;
        private System.Action<EquipSlot> _onLeftClick;
        private System.Action<EquipSlot> _onRightClick;

        private void Awake()
        {
            _eventTrigger = GetComponent<EventTrigger>();
            if (!_eventTrigger)
            {
                throw new NotFoundComponentException<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(OnClick);
            _eventTrigger.triggers.Add(entry);
        }

        public void Unequip()
        {
            if (defaultImage)
            {
                defaultImage.enabled = true;    
            }
            itemImage.enabled = false;
            gradeImage.enabled = false;
            item = null;
        }

        public void Set(ItemUsable equipment)
        {
            var sprite = equipment.GetIconSprite();
            if (defaultImage)
            {
                defaultImage.enabled = false;
            }
            itemImage.enabled = true;
            itemImage.overrideSprite = sprite;
            itemImage.SetNativeSize();
            item = equipment;

            var gradeSprite = equipment.GetBackgroundSprite();
            if (gradeSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(equipment.Data.grade.ToString());
            }

            gradeImage.enabled = true;
            gradeImage.overrideSprite = gradeSprite;
        }

        public void SetOnClickAction(System.Action<EquipSlot> onLeftClick, System.Action<EquipSlot> onRightClick)
        {
            _onLeftClick = onLeftClick;
            _onRightClick = onRightClick;
        }

        public void OnClick(BaseEventData eventData)
        {
            PointerEventData data = eventData as PointerEventData;

            if (!(data is null) && data.button == PointerEventData.InputButton.Left)
            {
                _onLeftClick?.Invoke(this);
            }
            else if (!(data is null) && data.button == PointerEventData.InputButton.Right)
            {
                _onRightClick?.Invoke(this);
            }
        }
    }
}
