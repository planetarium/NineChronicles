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

            switch (ItemSubType)
            {
                case ItemSubType.FullCostume:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                    break;
                case ItemSubType.HairCostume:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot;
                    break;
                case ItemSubType.EarCostume:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot;
                    break;
                case ItemSubType.EyeCostume:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot;
                    break;
                case ItemSubType.TailCostume:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot;
                    break;
                case ItemSubType.Title:
                    _requireLevel = GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                    break;
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

                    _cat = Widget.Find<MessageCatManager>().Show(true, _messageForCat, gameObject);
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

                foreach (var image in optionTagImages)
                {
                    image.gameObject.SetActive(false);
                }

                if (equip.optionCountFromCombination > 0)
                {
                    var data = optionTagData.GetOptionTagData(Item.Grade);
                    optionTagBg.gameObject.SetActive(true);
                    optionTagBg.range = data.GradeHsvRange;
                    optionTagBg.hue = data.GradeHsvHue;
                    optionTagBg.saturation = data.GradeHsvSaturation;
                    optionTagBg.value = data.GradeHsvValue;
                }

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
