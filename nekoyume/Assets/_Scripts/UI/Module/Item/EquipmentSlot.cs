using System;
using Assets.SimpleLocalization;
using Nekoyume.Model.Item;
using Nekoyume.UI.AnimatedGraphics;
using TMPro;
using UniRx;
using UniRx.Triggers;
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

        private int _requireLevel;
        private string _messageForCat;
        private MessageCat _cat;

        private EventTrigger _eventTrigger;
        private Action<EquipmentSlot> _onClick;
        private Action<EquipmentSlot> _onDoubleClick;

        public RectTransform RectTransform { get; private set; }
        public ItemSubType ItemSubType => itemSubType;
        public int ItemSubTypeIndex => itemSubTypeIndex;
        public ItemBase Item { get; private set; }
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

            switch (ItemSubType)
            {
                case ItemSubType.Weapon:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                    break;
                case ItemSubType.Armor:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                    break;
                case ItemSubType.Belt:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                    break;
                case ItemSubType.Necklace:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                    break;
                case ItemSubType.Ring:
                    _requireLevel = ItemSubTypeIndex == 1
                        ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                        : GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2;
                    break;
                case ItemSubType.Food:
                    switch (ItemSubTypeIndex)
                    {
                        case 1:
                            _requireLevel = GameConfig.RequireCharacterLevel
                                .CharacterConsumableSlot1;
                            break;
                        case 2:
                            _requireLevel = GameConfig.RequireCharacterLevel
                                .CharacterConsumableSlot2;
                            break;
                        case 3:
                            _requireLevel = GameConfig.RequireCharacterLevel
                                .CharacterConsumableSlot3;
                            break;
                        case 4:
                            _requireLevel = GameConfig.RequireCharacterLevel
                                .CharacterConsumableSlot4;
                            break;
                        case 5:
                            _requireLevel = GameConfig.RequireCharacterLevel
                                .CharacterConsumableSlot5;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _messageForCat =
                $"{LocalizationManager.Localize($"ITEM_SUB_TYPE_{ItemSubType.ToString()}")}\n<sprite name=\"UI_icon_lock_01\"> LV.{_requireLevel}";

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (!IsLock)
                    {
                        return;
                    }

                    if (_cat)
                    {
                        _cat.Hide();
                    }

                    _cat = Widget.Find<MessageCatManager>().Show(true, _messageForCat);
                })
                .AddTo(gameObject);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x =>
                {
                    if (!IsLock || !_cat)
                    {
                        return;
                    }

                    _cat.Hide();
                    _cat = null;
                })
                .AddTo(gameObject);
        }

        public void Set(
            ItemBase itemBase,
            Action<EquipmentSlot> onClick,
            Action<EquipmentSlot> onDoubleClick)
        {
            var sprite = itemBase.GetIconSprite();
            if (defaultImage)
            {
                defaultImage.enabled = false;
            }

            itemImage.enabled = true;
            itemImage.overrideSprite = sprite;
            itemImage.SetNativeSize();
            Item = itemBase;

            var gradeSprite = itemBase.GetBackgroundSprite();
            if (gradeSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(itemBase.Data.Grade.ToString());
            }

            gradeImage.enabled = true;
            gradeImage.overrideSprite = gradeSprite;

            if (itemBase is Equipment equip && equip.level > 0)
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

        public void Set(int avatarLevel)
        {
            if (avatarLevel < _requireLevel)
            {
                Lock();
            }
            else
            {
                Unlock();
            }
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

        private void Lock()
        {
            Clear();
            lockImage.gameObject.SetActive(true);
        }

        private void Unlock()
        {
            lockImage.gameObject.SetActive(false);
        }

        private void OnClick(BaseEventData eventData)
        {
            if (!(eventData is PointerEventData data) ||
                data.button != PointerEventData.InputButton.Left)
            {
                return;
            }

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
