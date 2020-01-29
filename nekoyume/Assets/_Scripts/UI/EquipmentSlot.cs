using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class EquipmentSlot : MonoBehaviour
    {
        public Image gradeImage;
        public Image defaultImage;
        public Image itemImage;
        public ItemUsable item;
        public TextMeshProUGUI enhancementText;
        public ItemSubType itemSubType;

        private EventTrigger _eventTrigger;
        private System.Action<EquipmentSlot> _onClick;
        private System.Action<EquipmentSlot> _onDoubleClick;
        
        public RectTransform RectTransform { get; private set; }

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
            
            RectTransform = GetComponent<RectTransform>();
        }

        public void Unequip()
        {
            if (defaultImage)
            {
                defaultImage.enabled = true;    
            }
            itemImage.enabled = false;
            gradeImage.enabled = false;
            enhancementText.enabled = false;
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
                throw new FailedToLoadResourceException<Sprite>(equipment.Data.Grade.ToString());
            }

            gradeImage.enabled = true;
            gradeImage.overrideSprite = gradeSprite;

            if(equipment is Equipment equip && equip.level > 0)
            {
                enhancementText.enabled = true;
                enhancementText.text = $"+{equip.level}";
            }
            else
            {
                enhancementText.enabled = false;
            }
        }

        public void SetOnClickAction(System.Action<EquipmentSlot> onClick, System.Action<EquipmentSlot> onDoubleClick)
        {
            _onClick = onClick;
            _onDoubleClick = onDoubleClick;
        }

        private void OnClick(BaseEventData eventData)
        {
            PointerEventData data = eventData as PointerEventData;

            if (data is null ||
                data.button != PointerEventData.InputButton.Left)
                return;
            
            switch (data.clickCount)
            {
                case 1:
                    _onClick?.Invoke(this);
                    break;
                case 2:
                    _onDoubleClick?.Invoke(this);
                    break;
            }
        }
    }
}
