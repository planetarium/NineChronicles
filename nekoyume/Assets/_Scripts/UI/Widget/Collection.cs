using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Coffee.UIEffects;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class Collection : Widget, IItemFilterOptionWidget
    {
        #region Internal Types
        [Serializable]
        private struct ItemTypeToggle
        {
            public ItemType type;
            public Toggle toggle;
            public Image hasNotificationImage;

            public void SetNotification(bool hasNotification)
            {
                hasNotificationImage.enabled = hasNotification;
            }
        }

        [Serializable]
        private struct StatToggle
        {
            public StatType stat;
            public Toggle toggle;
            public Image hasNotificationImage;

            public void SetNotification(bool hasNotification)
            {
                hasNotificationImage.enabled = hasNotification;
            }
        }

        private enum ESortType
        {
            None,
            Grade,
            Level,
        }
        #endregion Internal Types

        private const int SortingGroupWeight = 1000;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private ItemTypeToggle[] itemTypeToggles;

        [SerializeField]
        private StatToggle[] statToggles;

        [SerializeField]
        private CollectionEffect collectionEffect;

        [SerializeField]
        private CollectionScroll scroll;

        [SerializeField]
        private CollectionMaterialInfo collectionMaterialInfo;

        [Header("Center bottom")]
        [SerializeField]
        private TMP_Dropdown _sortDropdown;

        [SerializeField]
        private Button _sortButton;

        [SerializeField]
        private UIFlip _sortFlip;

        [SerializeField]
        private ESortType _sortType = ESortType.None;

        private bool _isSortDescending = true;

        private List<CollectionModel> _items;

        private CollectionMaterial _selectedMaterial;

        private ItemType _currentItemType;
        private StatType _currentStatType;

        private int? _initializedAvatarIndex = null;

        private ItemFilterOptions itemFilterOptions;

        private readonly List<CollectionModel> _models = new List<CollectionModel>();

        private readonly Dictionary<ItemType, Dictionary<StatType, bool>> _filter = new();
        private static readonly StatType[] TabStatTypes =
        {
            StatType.NONE,
            StatType.HP, StatType.ATK, StatType.DEF, StatType.HIT, StatType.SPD,
            StatType.CRI
        };

        public bool HasNotification => _filter.Values.Any(dict => dict.Values.Any(value => value));

        /// <summary>
        /// 필터 옵션 중 하나라도 활성화되었으면 true, 아니면 false
        /// </summary>
        private bool IsNeedFilter => IsNeedSearch || itemFilterOptions.IsNeedFilter;

        protected override void Awake()
        {
            base.Awake();

            backButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            foreach (var itemType in Enum.GetValues(typeof(ItemType)).OfType<ItemType>())
            {
                _filter[itemType] = new Dictionary<StatType, bool>();
                foreach (var statType in TabStatTypes)
                {
                    _filter[itemType][statType] = false;
                }
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.toggle.onClickToggle.AddListener(AudioController.PlayClick);
                itemTypeToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ =>
                    {
                        _currentItemType = itemTypeToggle.type;

                        var toggle = statToggles.First().toggle;
                        toggle.isOn = !toggle.isOn;

                        RefreshDropDownText();
                        UpdateStatToggleView();
                    })
                    .AddTo(gameObject);
            }

            foreach (var statToggle in statToggles)
            {
                statToggle.toggle.onClickToggle.AddListener(AudioController.PlayClick);
                statToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ =>
                    {
                        _currentStatType = statToggle.stat;

                        UpdateItems();
                    })
                    .AddTo(gameObject);
            }

            scroll.OnClickActiveButton.Subscribe(OnClickActiveButton).AddTo(gameObject);
            scroll.OnClickMaterial.Subscribe(SelectMaterial).AddTo(gameObject);
            collectionMaterialInfo.OnClickCloseButton
                .Subscribe(_ => SelectMaterial(null)).AddTo(gameObject);

            _sortButton.onClick.AddListener(OnClickSortButton);

            InitializeSortDropdown();
            RefreshDescendingUI();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);

            var toggle = itemTypeToggles.First().toggle;
            toggle.isOn = !toggle.isOn;

            UpdateToggleView();
            UpdateStatToggleView();
            UpdateItems();
        }

        public void TryInitialize()
        {
            var index = Game.Game.instance.States.CurrentAvatarKey;
            if (_initializedAvatarIndex == index)
            {
                return;
            }

            _initializedAvatarIndex = index;

            _models.Clear();
            _models.GenerateModels();

            UpdateEffectView();
            UpdateToggleDictionary();

            ReactiveAvatarState.Inventory.Subscribe(_ => OnUpdateInventory()).AddTo(gameObject);
        }

        private void RefreshDropDownText()
        {
            if (_sortDropdown.options.Count == 0)
                return;

            switch (_currentItemType)
            {
                case ItemType.Consumable:
                    _sortDropdown.options[(int)ESortType.Level].text = L10nManager.Localize("UI_COUNT");
                    break;
                case ItemType.Costume:
                    _sortDropdown.options[(int)ESortType.Level].text = L10nManager.Localize("UI_LEVEL");
                    break;
                case ItemType.Equipment:
                    _sortDropdown.options[(int)ESortType.Level].text = L10nManager.Localize("UI_LEVEL");
                    break;
                case ItemType.Material:
                    _sortDropdown.options[(int)ESortType.Level].text = L10nManager.Localize("UI_COUNT");
                    break;
            }
        }

        #region ScrollView

        private void UpdateItems()
        {
            _items = _models
                     .Where(model =>
                         model.ItemType == _currentItemType &&
                         model.Row.StatModifiers.Any(stat => IsInToggle(stat, _currentStatType)))
                     .ToList();

            _items.Sort(SortCollection);

            UpdateScrollView();
        }

        private void UpdateScrollView()
        {
            scroll.UpdateData(IsNeedFilter ? RefreshFilteredItems() : _items, true);
            SelectMaterial(null);
        }

        private void SelectMaterial(CollectionMaterial viewModel)
        {
            if (_selectedMaterial == null)
            {
                _selectedMaterial = viewModel;
                _selectedMaterial?.Selected.SetValueAndForceNotify(true);
            }
            else if (_selectedMaterial.Equals(viewModel))
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = null;
            }
            else
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = viewModel;
                _selectedMaterial?.Selected.SetValueAndForceNotify(true);
            }

            if (_selectedMaterial != null)
            {
                collectionMaterialInfo.Show(_selectedMaterial.Row);
            }
            else
            {
                collectionMaterialInfo.Close();
            }
        }

        private void OnClickActiveButton(CollectionModel model)
        {
            // check collection - is active
            var collectionId = model.Row.Id;
            var collectionState = Game.Game.instance.States.CollectionState;
            if (collectionState.Ids.Contains(collectionId))
            {
                Debug.LogError("collection already active");
                return;
            }

            void Action(List<ICollectionMaterial> materials)
            {
                ActionManager.Instance.ActivateCollection(collectionId, materials)
                    .Subscribe(eval =>
                    {
                        if (eval.Exception is not null)
                        {
                            OneLineSystem.Push(
                                MailType.System,
                                L10nManager.Localize("NOTIFICATION_COLLECTION_FAIL"),
                                NotificationCell.NotificationType.Alert);
                        }

                        LoadingHelper.ActivateCollection.Value = 0;
                    });

                LoadingHelper.ActivateCollection.Value = collectionId;
                AudioController.instance.PlaySfx(AudioController.SfxCode.Heal);
            }

            switch (model.ItemType)
            {
                case ItemType.Equipment:
                case ItemType.Costume:
                    Find<CollectionRegistrationPopup>().ShowForNonFungibleMaterial(model, Action);
                    break;
                case ItemType.Consumable:
                case ItemType.Material:
                    Find<CollectionRegistrationPopup>().ShowForFungibleMaterial(model, Action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        public void OnActionRender()
        {
            _models.UpdateActive(); // on action render

            UpdateEffectView(); // on update model.Active
        }

        private void OnUpdateInventory()
        {
            _models.UpdateMaterials(); // on update inventory

            UpdateToggleDictionary(); // on update model.CanActivate
            if (gameObject.activeSelf)
            {
                UpdateToggleView(); // on update toggleDictionary
                UpdateStatToggleView(); // on update toggleDictionary

                UpdateItems(); // on update model.CanActivate
            }
        }

        // on update model.Active
        private void UpdateEffectView()
        {
            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            collectionEffect.Set(
                collectionState.Ids.Count,
                collectionSheet.Count,
                collectionState.GetEffects(collectionSheet));
        }

        // on update model.CanActivate
        private void UpdateToggleDictionary()
        {
            // materials.Select(material => material.Active)로 전체 토글값을 Dict로 캐싱
            foreach (var (itemType, dict) in _filter)
            {
                foreach (var toggleStat in TabStatTypes)
                {
                    dict[toggleStat] = _models.Any(model =>
                        model.ItemType == itemType &&
                        model.Row.StatModifiers.Any(stat => IsInToggle(stat, toggleStat)) &&
                        model.CanActivate);
                }
            }
        }

        #region ToggleView

        private void UpdateToggleView()
        {
            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.SetNotification(
                    _filter[itemTypeToggle.type].Values.Any(value => value));
            }
        }

        private void UpdateStatToggleView()
        {
            foreach (var statToggle in statToggles)
            {
                statToggle.SetNotification(_filter[_currentItemType][statToggle.stat]);
            }
        }

        private static bool IsInToggle(StatModifier stat, StatType toggleStatType)
        {
            StatType[] etcTypes =
            {
                StatType.CRI,
                StatType.DRV, StatType.DRR, StatType.CDMG,
                StatType.ArmorPenetration, StatType.Thorn,
            };

            switch (toggleStatType)
            {
                // All
                case StatType.NONE:
                    return true;

                // StatTypes in Tab
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.SPD:
                    return toggleStatType == stat.StatType;

                // Etc
                default:
                    return etcTypes.Contains(stat.StatType);
            }
        }

        #endregion

        #region Sort
        private void RefreshDescendingUI()
        {
            _sortFlip.vertical = _isSortDescending;
        }

        private void OnClickSortButton()
        {
            _isSortDescending = !_isSortDescending;
            RefreshDescendingUI();
            UpdateItems();
        }

        private void InitializeSortDropdown()
        {
            _sortDropdown.ClearOptions();

            // TODO: Apply L10n
            var options = new List<string>();
            foreach (ESortType sortType in Enum.GetValues(typeof(ESortType)))
                options.Add(GetSortTypeString(sortType));
            _sortDropdown.AddOptions(options);

            _sortDropdown.onValueChanged.AddListener(OnSortDropdownValueChanged);
            RefreshDropDownText();
        }

        private string GetSortTypeString(ESortType sortType)
        {
            switch (sortType)
            {
                case ESortType.None:
                    return L10nManager.Localize("UI_RESET");
                case ESortType.Grade:
                    return L10nManager.Localize("UI_GRADE");
                case ESortType.Level:
                    return L10nManager.Localize("UI_LEVEL");
            }

            return string.Empty;
        }

        private void OnSortDropdownValueChanged(int index)
        {
            _sortType = (ESortType)index;
            UpdateItems();
        }

        private int SortCollection(CollectionModel a, CollectionModel b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            // 1. 활성화 가능
            if (a.CanActivate != b.CanActivate)
                return a.CanActivate ? -1 : 1;

            // 2. 재료 일정 부분 달성
            var aPartiallyActive = a.Materials.Any(CheckHasMaterialWhenInactive);
            var bPartiallyActive = b.Materials.Any(CheckHasMaterialWhenInactive);
            if (aPartiallyActive != bPartiallyActive)
                return aPartiallyActive ? -1 : 1;

            // 3. 재료 모두 미달성 (나머지)

            // 설정된 타입별로 정렬
            var sortByTypeValue = SortByType(a, b, _sortType);
            if (sortByTypeValue != 0)
                return sortByTypeValue;

            // 다른 조건이 같다면 ID로 비교
            if (a.Row.Id != b.Row.Id)
                return a.Row.Id < b.Row.Id ? -1 : 1;

            return 0;
        }

        private int SortByType(CollectionModel a, CollectionModel b, ESortType type)
        {
            var sortTypeWeight = _isSortDescending ? 1 : -1;
            switch (_sortType)
            {
                case ESortType.Grade:
                    var aGrade = a.Materials.Max(material => material.Grade);
                    var bGrade = b.Materials.Max(material => material.Grade);
                    return (bGrade - aGrade) * sortTypeWeight;
                case ESortType.Level:
                    var aLevel = a.Materials.Max(material => material.EnoughLevel);
                    var bLevel = b.Materials.Max(material => material.EnoughLevel);
                    return (bLevel - aLevel) * sortTypeWeight;
            }
            return 0;
        }

        private bool CheckHasMaterialWhenInactive(CollectionMaterial material)
        {
            if (material == null)
                return false;

            return material.HasItem && !material.Active;
        }
        #endregion Sort

        #region Filter
        private readonly List<CollectionModel> _filteredItems = new();

        private bool IsNeedSearch => !string.IsNullOrWhiteSpace(itemFilterOptions.SearchText);

        private List<CollectionModel> RefreshFilteredItems()
        {
            _filteredItems.Clear();

            foreach (var model in _items)
            {
                bool isContained = !(IsNeedSearch && !IsMatchedSearch(model));
                isContained &= ApplyFilterOption(model);
                if (isContained)
                    _filteredItems.Add(model);
            }

            return _filteredItems;
        }

        private bool ApplyFilterOption(CollectionModel model)
        {
            if (itemFilterOptions.Grade != ItemFilterPopupBase.Grade.All)
            {
                foreach (var material in model.Materials)
                {
                    var gradeFlag = (ItemFilterPopupBase.Grade)(1 << material.Grade);
                    var result = itemFilterOptions.Grade.HasFlag(gradeFlag);
                    if (!result)
                        return false;
                }
            }

            if (model.ItemType != ItemType.Equipment)
                return false;
            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;

            if (itemFilterOptions.Elemental != ItemFilterPopupBase.Elemental.All)
            {
                foreach (var material in model.Materials)
                {
                    if (!equipmentSheet.TryGetValue(material.Row.ItemId, out var equipment))
                        return false;

                    var elementalFlag = (ItemFilterPopupBase.Elemental)(1 << (int)equipment.ElementalType);
                    var result = itemFilterOptions.Elemental.HasFlag(elementalFlag);
                    if (!result)
                        return false;
                }
            }

            if (itemFilterOptions.UpgradeLevel != ItemFilterPopupBase.UpgradeLevel.All)
            {
                foreach (var material in model.Materials)
                {
                    var hasItem = false;
                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level1))
                        hasItem |= material.EnoughLevel == 1;

                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level2))
                        hasItem |= material.EnoughLevel == 2;

                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level3))
                        hasItem |= material.EnoughLevel == 3;

                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level4))
                        hasItem |= material.EnoughLevel == 4;

                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level5))
                        hasItem |= material.EnoughLevel == 5;

                    if (itemFilterOptions.UpgradeLevel.HasFlag(ItemFilterPopupBase.UpgradeLevel.Level6More))
                        hasItem |= material.EnoughLevel >= 6;

                    if (!hasItem)
                        return false;
                }
            }

            if (itemFilterOptions.ItemType != ItemFilterPopupBase.ItemType.All)
            {
                var result = itemFilterOptions.ItemType == (ItemFilterPopupBase.ItemType)model.ItemType;
                if (!result)
                    return false;
            }

            if (itemFilterOptions.UpgradeLevel != ItemFilterPopupBase.UpgradeLevel.All)
            {
                foreach (var material in model.Materials)
                {
                    var result = itemFilterOptions.UpgradeLevel.HasFlag((ItemFilterPopupBase.UpgradeLevel)(1 << material.EnoughLevel));
                    if (!result)
                        return false;
                }
            }

            if (itemFilterOptions.WithSkill != ItemFilterPopupBase.WithSkill.All)
            {
                // Collection에서 스킬 유무는 패스, 모든 아이템이 스킬이 있어야하기 때문에 필터링이 의미 없음
                return true;
            }

            return true;
        }

        private bool IsMatchedSearch(CollectionModel model)
        {
            if (model == null)
                return false;

            var itemName = L10nManager.LocalizeCollectionName(model.Row.Key);
            var nameMatched = Regex.IsMatch(itemName, itemFilterOptions.SearchText, RegexOptions.IgnoreCase);

            var materialMatched = false;
            foreach (var material in model.Materials)
            {
                var materialName = L10nManager.LocalizeItemName(material.Row.ItemId);
                if (!Regex.IsMatch(materialName, itemFilterOptions.SearchText, RegexOptions.IgnoreCase))
                    continue;
                materialMatched = true;
                break;
            }
            return nameMatched || materialMatched;
        }

        public void SetItemFilterOption(ItemFilterOptions type)
        {
            // TODO
            itemFilterOptions = type;
        }
        #endregion Filter
    }
}
