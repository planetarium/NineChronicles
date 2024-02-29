using System;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.AnimatedGraphics;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UIEffects;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.State;

namespace Nekoyume.UI.Module
{
    using System.Linq;
    using UniRx;

    // TODO: 지금의 `EquipmentSlot`은 장비 뿐만 아니라 소모품과 코스튬이 모두 사용하고 있습니다.
    // 이것을 각각의 아이템 타입에 맞게 일반화할 필요가 있습니다.
    // 이에 따라 `EquipmentSlots`도 함께 수정될 필요가 있습니다.
    [RequireComponent(typeof(RectTransform))]
    public class EquipmentSlot : MonoBehaviour
    {
        private static readonly Color OriginColor = Color.white;
        private static readonly Color DimmedColor = ColorHelper.HexToColorRGB("848484");

        [SerializeField]
        protected OptionTagDataScriptableObject optionTagData = null;

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
        private ItemType itemType = ItemType.Equipment;

        [SerializeField]
        private int itemSubTypeIndex = 1;

        [SerializeField]
        protected UIHsvModifier optionTagBg = null;

        [SerializeField]
        protected List<Image> optionTagImages = null;

        [SerializeField]
        protected ItemViewDataScriptableObject itemViewData;

        [SerializeField]
        protected Image gradeImage;

        [SerializeField]
        protected UIHsvModifier gradeHsv;

        [SerializeField]
        protected Image enhancementImage;

        private int _requireLevel;
        private string _messageForCat;
        private MessageCat _cat;

        private EventTrigger _eventTrigger;
        private Action<EquipmentSlot> _onClick;
        private Action<EquipmentSlot> _onDoubleClick;

        public RectTransform RectTransform { get; private set; }
        public ItemSubType ItemSubType => itemSubType;
        public ItemType ItemType => itemType;
        public int ItemSubTypeIndex => itemSubTypeIndex;
        public ItemBase Item { get; private set; }
        public bool ShowUnlockTooltip { get; set; }
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

            UpdateRequireLevel();

            _messageForCat =
                $"{L10nManager.Localize($"ITEM_SUB_TYPE_{ItemSubType.ToString()}")}\n<sprite name=\"UI_icon_lock_01\"> LV.{_requireLevel}";

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (!ShowUnlockTooltip || !IsLock)
                    {
                        return;
                    }

                    if (_cat)
                    {
                        _cat.Hide();
                    }

                    _cat = Widget.Find<MessageCatTooltip>().Show(true, _messageForCat, gameObject);
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

        private void UpdateRequireLevel()
        {
            var gameConfig = States.Instance.GameConfigState;

            switch (ItemSubType)
            {
                case ItemSubType.FullCostume:
                    _requireLevel = gameConfig.RequireCharacterLevel_FullCostumeSlot;
                    break;
                case ItemSubType.HairCostume:
                    _requireLevel = gameConfig.RequireCharacterLevel_HairCostumeSlot;
                    break;
                case ItemSubType.EarCostume:
                    _requireLevel = gameConfig.RequireCharacterLevel_EarCostumeSlot;
                    break;
                case ItemSubType.EyeCostume:
                    _requireLevel = gameConfig.RequireCharacterLevel_EyeCostumeSlot;
                    break;
                case ItemSubType.TailCostume:
                    _requireLevel = gameConfig.RequireCharacterLevel_TailCostumeSlot;
                    break;
                case ItemSubType.Title:
                    _requireLevel = gameConfig.RequireCharacterLevel_TitleSlot;
                    break;
                case ItemSubType.Weapon:
                    _requireLevel = gameConfig.RequireCharacterLevel_EquipmentSlotWeapon;
                    break;
                case ItemSubType.Armor:
                    _requireLevel = gameConfig.RequireCharacterLevel_EquipmentSlotArmor;
                    break;
                case ItemSubType.Belt:
                    _requireLevel = gameConfig.RequireCharacterLevel_EquipmentSlotBelt;
                    break;
                case ItemSubType.Necklace:
                    _requireLevel = gameConfig.RequireCharacterLevel_EquipmentSlotNecklace;
                    break;
                case ItemSubType.Ring:
                    _requireLevel = ItemSubTypeIndex == 1
                        ? gameConfig.RequireCharacterLevel_EquipmentSlotRing1
                        : gameConfig.RequireCharacterLevel_EquipmentSlotRing2;
                    break;
                case ItemSubType.Aura:
                    _requireLevel = gameConfig.RequireCharacterLevel_EquipmentSlotAura;
                    break;
                case ItemSubType.Food:
                    switch (ItemSubTypeIndex)
                    {
                        case 1:
                            _requireLevel = gameConfig.RequireCharacterLevel_ConsumableSlot1;
                            break;
                        case 2:
                            _requireLevel = gameConfig.RequireCharacterLevel_ConsumableSlot2;
                            break;
                        case 3:
                            _requireLevel = gameConfig.RequireCharacterLevel_ConsumableSlot3;
                            break;
                        case 4:
                            _requireLevel = gameConfig.RequireCharacterLevel_ConsumableSlot4;
                            break;
                        case 5:
                            _requireLevel = gameConfig.RequireCharacterLevel_ConsumableSlot5;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState != null)
            {
                Set(avatarState.level);
            }
        }

        public void Set(
            ItemBase itemBase,
            Action<EquipmentSlot> onClick,
            Action<EquipmentSlot> onDoubleClick)
        {
            if (itemBase is null)
            {
                Clear();

                _onClick = onClick;
                _onDoubleClick = onDoubleClick;
                return;
            }

            var sprite = itemBase.GetIconSprite();
            if (defaultImage)
            {
                defaultImage.enabled = false;
            }

            itemImage.enabled = true;
            itemImage.overrideSprite = sprite;
            itemImage.SetNativeSize();
            Item = itemBase;

            gradeImage.enabled = true;
            var gradeData = itemViewData.GetItemViewData(itemBase.Grade);
            gradeImage.overrideSprite = gradeData.GradeBackground;
            gradeHsv.range = gradeData.GradeHsvRange;
            gradeHsv.hue = gradeData.GradeHsvHue;
            gradeHsv.saturation = gradeData.GradeHsvSaturation;
            gradeHsv.value = gradeData.GradeHsvValue;

            optionTagBg.gameObject.SetActive(false);
            enhancementImage.gameObject.SetActive(false);
            if (itemBase is Equipment equip)
            {
                var isUpgraded = equip.level > 0;
                enhancementText.enabled = isUpgraded;
                if (isUpgraded)
                {
                    enhancementText.text = $"+{equip.level}";
                }

                if (equip.level >= Util.VisibleEnhancementEffectLevel)
                {
                    enhancementImage.gameObject.SetActive(true);
                    enhancementImage.material = gradeData.EnhancementMaterial;
                }

                if (equip.GetOptionCountFromCombination() <= 0)
                {
                    optionTagBg.gameObject.SetActive(false);
                    return;
                }

                foreach (var image in optionTagImages)
                {
                    image.gameObject.SetActive(false);
                }

                var data = optionTagData.GetOptionTagData(Item.Grade);
                optionTagBg.gameObject.SetActive(true);
                optionTagBg.range = data.GradeHsvRange;
                optionTagBg.hue = data.GradeHsvHue;
                optionTagBg.saturation = data.GradeHsvSaturation;
                optionTagBg.value = data.GradeHsvValue;

                var optionInfo = new ItemOptionInfo(Item as Equipment);
                var optionCount = optionInfo.StatOptions.Sum(x => x.count);
                var index = 0;
                for (var i = 0; i < optionCount; ++i)
                {
                    var image = optionTagImages[index];
                    image.gameObject.SetActive(true);
                    image.sprite = optionTagData.StatOptionSprite;
                    ++index;
                }

                for (var i = 0; i < optionInfo.SkillOptions.Count; ++i)
                {
                    var image = optionTagImages[index];
                    image.gameObject.SetActive(true);
                    image.sprite = optionTagData.SkillOptionSprite;
                    ++index;
                }
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

        public void SetDim(bool isDim)
        {
            if (Item is {ItemType: ItemType.Equipment})
            {
                isDim |= Util.GetItemRequirementLevel(Item) >
                         States.Instance.CurrentAvatarState.level;
            }

            gradeImage.color = isDim ? DimmedColor : OriginColor;
            enhancementText.color = isDim ? DimmedColor : OriginColor;
            itemImage.color = isDim ? DimmedColor : OriginColor;
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
            enhancementImage.gameObject.SetActive(false);
            optionTagBg.gameObject.SetActive(false);
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
                    AudioController.PlayClick();
                    break;
                case 2:
                    _onDoubleClick?.Invoke(this);
                    // note : _onDoubleClick has another sfx, not `sfx_click`.
                    break;
            }
        }
    }
}
