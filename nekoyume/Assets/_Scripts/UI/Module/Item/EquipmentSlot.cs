using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(RectTransform))]
    public class EquipmentSlot : MonoBehaviour
    {
        [SerializeField]
        private Image gradeImage = null;

        [SerializeField]
        private Image defaultImage = null;

        [SerializeField]
        private Image itemImage = null;

        [SerializeField]
        private TextMeshProUGUI enhancementText = null;

        [SerializeField]
        private Image lockImage = null;

        [SerializeField]
        private ItemSubType itemSubType = ItemSubType.Armor;

        [SerializeField]
        private int itemSubTypeIndex = 1;

        private EventTrigger _eventTrigger;
        private System.Action<EquipmentSlot> _onClick;
        private System.Action<EquipmentSlot> _onDoubleClick;

        public RectTransform RectTransform { get; private set; }
        public ItemSubType ItemSubType => itemSubType;
        public int ItemSubTypeIndex => itemSubTypeIndex;
        public ItemUsable Item { get; private set; }
        public bool IsLock => lockImage.gameObject.activeSelf;
        public bool IsEmpty => Item is null;

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

        public void Set(ItemUsable equipment)
        {
            Set(equipment, _onClick, _onDoubleClick);
        }
        
        public void Set(ItemUsable equipment, System.Action<EquipmentSlot> onClick, System.Action<EquipmentSlot> onDoubleClick)
        {
            var sprite = equipment.GetIconSprite();
            if (defaultImage)
            {
                defaultImage.enabled = false;
            }

            itemImage.enabled = true;
            itemImage.overrideSprite = sprite;
            itemImage.SetNativeSize();
            Item = equipment;

            var gradeSprite = equipment.GetBackgroundSprite();
            if (gradeSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(equipment.Data.Grade.ToString());
            }

            gradeImage.enabled = true;
            gradeImage.overrideSprite = gradeSprite;

            if (equipment is Equipment equip && equip.level > 0)
            {
                enhancementText.enabled = true;
                enhancementText.text = $"+{equip.level}";
            }
            else
            {
                enhancementText.enabled = false;
            }
            
            _onClick = onClick;
            _onDoubleClick = onDoubleClick;
        }
        
        public void Clear()
        {
            if (defaultImage)
            {
                defaultImage.enabled = true;
            }

            itemImage.enabled = false;
            gradeImage.enabled = false;
            enhancementText.enabled = false;
            Item = null;
            Unlock();
        }

        public void Lock()
        {
            Clear();
            lockImage.gameObject.SetActive(true);
        }

        public void Unlock()
        {
            lockImage.gameObject.SetActive(false);
        }

        private void OnClick(BaseEventData eventData)
        {
            if (!(eventData is PointerEventData data) ||
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
