using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.Helper;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;
using Nekoyume.State;
using System.Numerics;
using Coffee.UIEffects;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.TableData.Event;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;
using ToggleGroup = UnityEngine.UI.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class SubRecipeView : MonoBehaviour
    {
        public struct RecipeInfo
        {
            public int RecipeId;
            public int? SubRecipeId;
            public BigInteger CostNCG;
            public FungibleAssetValue CostCrystal;
            public int CostAP;
            public Dictionary<int, int> Materials;
            public Dictionary<int, int> ReplacedMaterials;
        }

        [Serializable]
        public struct RecipeTabGroup
        {
            [Serializable]
            public struct RecipeTab
            {
                public Toggle toggle;
                public TextMeshProUGUI disableText;
                public TextMeshProUGUI enableText;
            }

            public ToggleGroup toggleGroup;
            public List<RecipeTab> recipeTabs;
        }

        [Serializable]
        private struct HammerPointView
        {
            public GameObject parentObject;
            public Slider nowPoint;
            public Image nowPointImage;
            public Image increasePointImage;
            public TMP_Text hammerPointText;
            public GameObject notEnoughHammerPointObject;
            public GameObject enoughHammerPointObject;
            public Button superCraftButton;
        }

        [Serializable]
        public class RequiredNormalItemIcon
        {
            public ArenaType arenaType;
            public Sprite    sprite;
        }

        [SerializeField]
        private RecipeCell recipeCell;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI blockIndexText;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView;

        [SerializeField]
        private ConditionalCostButton button;

        [SerializeField]
        private GameObject lockedObject;

        [SerializeField]
        private TextMeshProUGUI lockedText;

        [SerializeField]
        private TextMeshProUGUI[] mainStatTexts;

        [SerializeField] [Header("[Equipment]")]
        private RecipeTabGroup normalRecipeTabGroup;

        [SerializeField]
        private GameObject premiumCraftableIcon;

        [SerializeField]
        private RecipeTabGroup legendaryRecipeTabGroup;

        [SerializeField]
        private RecipeOptionView optionView;

        [SerializeField]
        private HammerPointView hammerPointView;

        [SerializeField]
        private UIHsvModifier[] bgHsvModifiers;

        [SerializeField] [Header("[EventMaterial]")]
        private ConditionalCostButton materialSelectButton;

        [SerializeField]
        private List<RequiredNormalItemIcon> requiredNormalItemIcons;

        [SerializeField]
        private Image requiredNormalItemImage;

        [SerializeField]
        private TextMeshProUGUI collectionCount;

        public readonly Subject<RecipeInfo> CombinationActionSubject = new Subject<RecipeInfo>();

        private SheetRow<int> _recipeRow;
        private List<int> _subrecipeIds;
        private int _selectedIndex;
        private RecipeInfo _selectedRecipeInfo;

        private const string StatTextFormat = "{0} {1}";
        private const int PremiumRecipeIndex = 1;
        private const int MimisbrunnrRecipeIndex = 2;
        private IDisposable _disposableForOnDisable;

        private bool _canSuperCraft;
        private HammerPointState _hammerPointState;

        public static string[] DefaultTabNames = new string[]{ "A", "B", "C" };

        private void Awake()
        {
            for (int i = 0; i < normalRecipeTabGroup.recipeTabs.Count; ++i)
            {
                var innerIndex = i;
                var tab        = normalRecipeTabGroup.recipeTabs[i];
                tab.toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ChangeTab(innerIndex);
                });
            }

            for (int i = 0; i < legendaryRecipeTabGroup.recipeTabs.Count; ++i)
            {
                var innerIndex = i;
                var tab        = legendaryRecipeTabGroup.recipeTabs[i];
                tab.toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ChangeTab(innerIndex);
                });
            }

            if (button != null)
            {
                button.OnClickSubject
                    .Subscribe(state => CombineCurrentRecipe())
                    .AddTo(gameObject);

                button.OnClickDisabledSubject
                    .Subscribe(_ =>
                    {
                        if (!CheckSubmittable(out var errorMessage, out var slotIndex))
                        {
                            OneLineSystem.Push(MailType.System, errorMessage,
                                               NotificationCell.NotificationType.Alert);
                        }
                    })
                    .AddTo(gameObject);
            }

            if (hammerPointView.superCraftButton)
            {
                hammerPointView.superCraftButton
                    .OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        Widget.Find<SuperCraftPopup>().Show(
                            (EquipmentItemRecipeSheet.Row)_recipeRow,
                            _canSuperCraft);
                    }).AddTo(gameObject);
            }

            if (materialSelectButton != null)
            {
                materialSelectButton.OnClickSubject
                    .Subscribe(_ =>
                    {
                        Widget.Find<ItemMaterialSelectPopup>().Show(
                            (EventMaterialItemRecipeSheet.Row)_recipeRow,
                            materials =>
                            {
                                _selectedRecipeInfo.Materials = materials;
                                CombineCurrentRecipe();
                            });
                    }).AddTo(gameObject);
                UpdateButtonForEventMaterial();
            }
        }

        private void OnDisable()
        {
            if (_disposableForOnDisable != null)
            {
                _disposableForOnDisable.Dispose();
                _disposableForOnDisable = null;
            }
        }

        public void SetData(SheetRow<int> recipeRow, List<int> subRecipeIds)
        {
            _recipeRow    = recipeRow;
            _subrecipeIds = subRecipeIds;

            string title       = null;
            var    isEquipment = false;
            switch (recipeRow)
            {
                case EquipmentItemRecipeSheet.Row equipmentRow:
                {
                    isEquipment = true;
                    var resultItem = equipmentRow.GetResultEquipmentItemRow();
                    title = resultItem.GetLocalizedName(false, false);

                    for (var i = 0; i < mainStatTexts.Length; i++)
                    {
                        var mainStatText = mainStatTexts[i];
                        if (i == 0)
                        {
                            var stat          = resultItem.GetUniqueStat();
                            var statValueText = stat.StatType.ValueToString(stat.TotalValueAsLong);
                            mainStatText.text =
                                string.Format(StatTextFormat, stat.StatType, statValueText);
                            mainStatText.gameObject.SetActive(true);
                        }
                        else if (i == 1 && Util.IsEventEquipmentRecipe(equipmentRow.Id))
                        {
                            mainStatText.text = resultItem.GetLocalizedDescription();
                            mainStatText.gameObject.SetActive(true);
                        }
                        else
                        {
                            mainStatText.gameObject.SetActive(false);
                        }
                    }

                    if (Util.IsEventEquipmentRecipe(equipmentRow.Id))
                    {
                        recipeCell.Show(equipmentRow, false);
                        UpdateCollectionCount(resultItem.Id);
                        ChangeTab(0);
                        break;
                    }

                    if (_subrecipeIds != null && _subrecipeIds.Any())
                    {
                        var isNormalRecipe = resultItem.Grade < 5;
                        normalRecipeTabGroup.toggleGroup.gameObject.SetActive(isNormalRecipe);
                        legendaryRecipeTabGroup.toggleGroup.gameObject.SetActive(!isNormalRecipe);

                        if (!isNormalRecipe)
                        {
                            var tabNames = DefaultTabNames;
                            var tab = Craft.SubRecipeTabs.FirstOrDefault(
                                tab => tab.RecipeId == _recipeRow.Key);
                            if (tab != null)
                            {
                                tabNames = tab.TabNames;
                            }

                            for (int i = 0; i < legendaryRecipeTabGroup.recipeTabs.Count; i++)
                            {
                                var recipeTab = legendaryRecipeTabGroup.recipeTabs[i];

                                recipeTab.toggle.gameObject.SetActive(i < tabNames.Length);
                                if (i < tabNames.Length)
                                {
                                    recipeTab.disableText.text = tabNames[i];
                                    recipeTab.enableText.text  = tabNames[i];
                                }
                            }
                        }
                        else
                        {
                            var craftable =
                                CheckCraftableSubRecipe(equipmentRow, PremiumRecipeIndex);
                            premiumCraftableIcon.gameObject.SetActive(craftable);
                        }

                        var recipeGroup = isNormalRecipe
                            ? normalRecipeTabGroup
                            : legendaryRecipeTabGroup;
                        if (recipeGroup.recipeTabs.Any())
                        {
                            var selectedRecipeTab = recipeGroup.recipeTabs[0];
                            if (selectedRecipeTab.toggle.isOn)
                            {
                                ChangeTab(0);
                            }
                            else
                            {
                                selectedRecipeTab.toggle.isOn = true;
                            }
                        }
                    }
                    else
                    {
                        normalRecipeTabGroup.toggleGroup.gameObject.SetActive(false);
                        legendaryRecipeTabGroup.toggleGroup.gameObject.SetActive(false);
                    }

                    recipeCell.Show(equipmentRow, false);
                    UpdateCollectionCount(resultItem.Id);
                    break;
                }
                case ConsumableItemRecipeSheet.Row consumableRow:
                {
                    var resultItem = consumableRow.GetResultConsumableItemRow();
                    title = resultItem.GetLocalizedName(false);

                    var statsCount = resultItem.Stats.Count;
                    for (var i = 0; i < mainStatTexts.Length; i++)
                    {
                        var mainStatText = mainStatTexts[i];
                        if (i < statsCount)
                        {
                            var stat          = resultItem.Stats[i];
                            var statValueText = stat.StatType.ValueToString(stat.TotalValueAsLong);
                            mainStatText.text =
                                string.Format(StatTextFormat, stat.StatType, statValueText);
                            mainStatText.gameObject.SetActive(true);
                            continue;
                        }

                        mainStatText.gameObject.SetActive(false);
                    }

                    recipeCell.Show(consumableRow, false);
                    ChangeTab(0);
                    UpdateCollectionCount(resultItem.Id);
                    break;
                }
                case EventMaterialItemRecipeSheet.Row materialRow:
                {
                    var resultItem = materialRow.GetResultMaterialItemRow();
                    title                      = resultItem.GetLocalizedName(false, false);
                    mainStatTexts.First().text = resultItem.GetLocalizedDescription();
                    recipeCell.Show(materialRow, false);
                    ChangeTab(0);
                    UpdateCollectionCount(resultItem.Id);
                    break;
                }
            }

            titleText.text = title;

            if (_disposableForOnDisable != null)
            {
                _disposableForOnDisable.Dispose();
                _disposableForOnDisable = null;
            }

            if (isEquipment)
            {
                _disposableForOnDisable = Craft.SharedModel.UnlockedRecipes.Subscribe(_ =>
                {
                    if (Craft.SharedModel.UnlockedRecipes.HasValue &&
                        gameObject.activeSelf)
                    {
                        UpdateButtonForEquipment();
                    }
                });
            }
        }

        public void UpdateCollectionCount(int itemId)
        {
            if (collectionCount == null)
            {
                return;
            }

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionsByRecipeItem = collectionSheet.Values
                .Where(row => row.Materials.Any(material => material.ItemId == itemId)).ToList();
            var maxCount = collectionsByRecipeItem.Count;
            var count = collectionState.Ids.Count(activated =>
                                                      collectionsByRecipeItem.Any(
                                                          row => row.Id == activated));
            collectionCount.gameObject.transform.parent.gameObject.SetActive(maxCount != 0);
            collectionCount.text = $"{count}/{maxCount}";
        }

        public void ResetSelectedIndex()
        {
            _selectedIndex = 0;
        }

        public void UpdateView()
        {
            ChangeTab(_selectedIndex);
        }

        private void ChangeTab(int index)
        {
            _selectedIndex = index;
            UpdateInformation(index);
        }

        // *DO NOT USE AT NO TUTORIAL*
        public void ChangeTabToPremiumForTutorial()
        {
            normalRecipeTabGroup.recipeTabs[1].toggle.isOn = true;
        }

        private RecipeInfo GetEquipmentRecipeInfo(EquipmentItemRecipeSheet.Row equipmentRow,
            int index, out long blockIndex, out long maxBlockIndex)
        {
            var recipeInfo = new RecipeInfo();
            var materials  = new Dictionary<int, int>();
            maxBlockIndex = 0;

            var isUnlocked = Craft.SharedModel.UnlockedRecipes.Value.Contains(_recipeRow.Key)
                             || equipmentRow.CRYSTAL == 0;
            var baseMaterialInfo = new EquipmentItemSubRecipeSheet.MaterialInfo(
                equipmentRow.MaterialId,
                equipmentRow.MaterialCount);
            blockIndex = equipmentRow.RequiredBlockIndex;

            recipeInfo.CostNCG  = equipmentRow.RequiredGold;
            recipeInfo.CostAP   = equipmentRow.RequiredActionPoint;
            recipeInfo.RecipeId = equipmentRow.Id;

            // Add base material
            materials.Add(equipmentRow.MaterialId, equipmentRow.MaterialCount);

            if (_subrecipeIds != null && _subrecipeIds.Any())
            {
                recipeInfo.SubRecipeId = _subrecipeIds[index];
                var subRecipe = TableSheets.Instance
                    .EquipmentItemSubRecipeSheetV2[recipeInfo.SubRecipeId.Value];
                var options = subRecipe.Options;

                blockIndex    += subRecipe.RequiredBlockIndex;
                maxBlockIndex =  blockIndex + options.Sum(o => o.RequiredBlockIndex);

                var isEventEquipment = Util.IsEventEquipmentRecipe(recipeInfo.RecipeId);
                if (!isEventEquipment)
                {
                    var isPremium = index == PremiumRecipeIndex &&
                                    equipmentRow.GetResultEquipmentItemRow().Grade < 5;

                    Array.ForEach(bgHsvModifiers, modifier => modifier.enabled = isPremium);
                    optionView.SetOptions(options, isPremium);

                    var isMimisbrunnrSubRecipe = index == MimisbrunnrRecipeIndex &&
                                                 (subRecipe.IsMimisbrunnrSubRecipe ?? true);
                    var hammerPointStates = States.Instance.HammerPointStates;
                    var showHammerPoint = hammerPointStates is not null &&
                                          hammerPointStates.TryGetValue(
                                              recipeInfo.RecipeId, out _hammerPointState) &&
                                          !isMimisbrunnrSubRecipe;

                    hammerPointView.parentObject.SetActive(showHammerPoint);
                    if (showHammerPoint)
                    {
                        var max = TableSheets.Instance.CrystalHammerPointSheet[recipeInfo.RecipeId]
                            .MaxPoint;
                        var increasePoint = subRecipe.RewardHammerPoint ?? 1;
                        var increasedPoint =
                            Math.Min(_hammerPointState.HammerPoint + increasePoint, max);
                        _canSuperCraft                    = _hammerPointState.HammerPoint == max;
                        hammerPointView.nowPoint.maxValue = max;
                        hammerPointView.hammerPointText.text =
                            $"{_hammerPointState.HammerPoint}/{max}";
                        hammerPointView.nowPoint.value = _hammerPointState.HammerPoint;
                        hammerPointView.nowPointImage.fillAmount =
                            _hammerPointState.HammerPoint / (float)max;
                        hammerPointView.increasePointImage.fillAmount = increasedPoint / (float)max;
                        hammerPointView.notEnoughHammerPointObject.SetActive(!_canSuperCraft);
                        hammerPointView.enoughHammerPointObject.SetActive(_canSuperCraft);
                    }

                    var sheet = TableSheets.Instance.ItemRequirementSheet;
                    if (!sheet.TryGetValue(equipmentRow.ResultEquipmentId, out var row))
                    {
                        levelText.enabled = false;
                    }
                    else
                    {
                        var level = isMimisbrunnrSubRecipe ? row.MimisLevel : row.Level;
                        levelText.text    = $"Lv {level}";
                        levelText.enabled = true;
                    }

                    requiredItemRecipeView.SetData(
                        baseMaterialInfo,
                        subRecipe.Materials,
                        true,
                        !isUnlocked);
                }
                else
                {
                    var list = new List<EquipmentItemSubRecipeSheet.MaterialInfo>
                        { baseMaterialInfo };
                    list.AddRange(subRecipe.Materials);
                    requiredItemRecipeView.SetData(list, true);
                }

                recipeInfo.CostNCG += subRecipe.RequiredGold;

                foreach (var material in subRecipe.Materials)
                {
                    materials.Add(material.Id, material.Count);
                }
            }
            else
            {
                requiredItemRecipeView.SetData(baseMaterialInfo, null, true, !isUnlocked);
            }

            recipeInfo.Materials         = materials;
            recipeInfo.ReplacedMaterials = GetReplacedMaterials(materials);

            return recipeInfo;
        }

        private RecipeInfo GetConsumableRecipeInfo(ConsumableItemRecipeSheet.Row consumableRow, out long blockIndex)
        {
            var recipeInfo = new RecipeInfo();
            var materials  = new Dictionary<int, int>();

            blockIndex = consumableRow.RequiredBlockIndex;
            requiredItemRecipeView.SetData(consumableRow.Materials, true);
            recipeInfo.CostNCG  = (BigInteger)consumableRow.RequiredGold;
            recipeInfo.CostAP   = consumableRow.RequiredActionPoint;
            recipeInfo.RecipeId = consumableRow.Id;

            var sheet = TableSheets.Instance.ItemRequirementSheet;
            if (!sheet.TryGetValue(consumableRow.ResultConsumableItemId, out var row))
            {
                levelText.enabled = false;
            }
            else
            {
                levelText.text    = $"Lv {row.Level}";
                levelText.enabled = true;
            }

            foreach (var material in consumableRow.Materials)
            {
                materials.Add(material.Id, material.Count);
            }

            recipeInfo.Materials         = materials;
            recipeInfo.ReplacedMaterials = GetReplacedMaterials(materials);

            return recipeInfo;
        }

        private RecipeInfo GetEventMaterialRecipeInfo(EventMaterialItemRecipeSheet.Row eventMaterialRow,
            out long blockIndex)
        {
            var recipeInfo = new RecipeInfo();
            var materials  = new Dictionary<int, int>();

            blockIndex = 1;
            requiredItemRecipeView.SetData(
                eventMaterialRow.RequiredMaterialsId,
                eventMaterialRow.RequiredMaterialsCount);
            recipeInfo.RecipeId = eventMaterialRow.Id;

            var defaultItemSprite = requiredNormalItemIcons.First().sprite;
            if (TableSheets.Instance.ArenaSheet.TryGetArenaType(
                    eventMaterialRow.RequiredMaterialsId.First(), out var arenaType))
            {
                var itemSprite = requiredNormalItemIcons
                    .FirstOrDefault(icon => icon.arenaType == arenaType)?.sprite;
                requiredNormalItemImage.overrideSprite = itemSprite ? itemSprite : defaultItemSprite;
            }
            else
            {
                requiredNormalItemImage.overrideSprite = defaultItemSprite;
            }

            recipeInfo.Materials         = materials;
            recipeInfo.ReplacedMaterials = GetReplacedMaterials(materials);

            return recipeInfo;
        }

        private void SetExpText(EquipmentItemRecipeSheet.Row equipmentRow)
        {
            if (expText == null)
            {
                return;
            }

            var equipment = equipmentRow.GetResultEquipmentItemRow();

            if (equipment.Exp == null)
            {
                expText.text = string.Empty;
                return;
            }

            var value = equipment.Exp.Value;
            expText.text = $"EXP {value.ToCurrencyNotation()}";
        }

        private void UpdateInformation(int index)
        {
            long blockIndex = 0;
            long maxBlockIndex = 0;

            switch (_recipeRow)
            {
                case EquipmentItemRecipeSheet.Row equipmentRow:
                    _selectedRecipeInfo = GetEquipmentRecipeInfo(equipmentRow, index, out blockIndex, out maxBlockIndex);
                    UpdateButtonForEquipment();
                    SetExpText(equipmentRow);
                    break;

                case ConsumableItemRecipeSheet.Row consumableRow:
                    _selectedRecipeInfo = GetConsumableRecipeInfo(consumableRow, out blockIndex);
                    UpdateButtonForConsumable();
                    break;

                case EventMaterialItemRecipeSheet.Row eventMaterialRow:
                    _selectedRecipeInfo = GetEventMaterialRecipeInfo(eventMaterialRow, out blockIndex);
                    break;
            }

            blockIndexText.text = $"{blockIndex}{(maxBlockIndex > 0 ? $" ~ {maxBlockIndex}" : string.Empty)}";
        }

        private static Dictionary<int, int> GetReplacedMaterials(Dictionary<int, int> required)
        {
            var replacedMaterialMap = new Dictionary<int, int>();
            var inventory = States.Instance.CurrentAvatarState.inventory;

            foreach (var (id, count) in required)
            {
                if (!TableSheets.Instance.MaterialItemSheet.TryGetValue(id, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(
                    row.ItemId,
                    out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                if (count > itemCount)
                {
                    replacedMaterialMap.Add(row.Id, count - itemCount);
                }
            }

            return replacedMaterialMap;
        }

        private void UpdateButtonForEquipment()
        {
            button.Interactable = false;
            if (_selectedRecipeInfo.Equals(default))
            {
                return;
            }

            var isUnlocked = Craft.SharedModel.UnlockedRecipes.Value.Contains(_selectedRecipeInfo.RecipeId) ||
                             TableSheets.Instance.EquipmentItemRecipeSheet[_selectedRecipeInfo.RecipeId].CRYSTAL == 0;
            if (isUnlocked)
            {
                var submittable = CheckNCGAndSlotIsEnough();
                var costNCG = new ConditionalCostButton.CostParam(
                    CostType.NCG,
                    (long)_selectedRecipeInfo.CostNCG);
                var sheet = TableSheets.Instance.CrystalMaterialCostSheet;

                var crystalCost = 0 * CrystalCalculator.CRYSTAL;
                foreach (var pair in _selectedRecipeInfo.ReplacedMaterials)
                {
                    try
                    {
                        crystalCost += CrystalCalculator.CalculateMaterialCost(pair.Key, pair.Value, sheet);
                    }
                    catch (ArgumentException)
                    {
                        submittable = false;
                        continue;
                    }
                }

                _selectedRecipeInfo.CostCrystal = crystalCost;
                var costCrystal = new ConditionalCostButton.CostParam(
                    CostType.Crystal,
                    (long)crystalCost.MajorUnit);

                button.SetCost(costNCG, costCrystal);
                button.Interactable = submittable;
                button.gameObject.SetActive(true);
                lockedObject.SetActive(false);
            }
            else if (Craft.SharedModel.UnlockingRecipes.Contains(_selectedRecipeInfo.RecipeId))
            {
                button.gameObject.SetActive(false);
                lockedObject.SetActive(true);
                lockedText.text = L10nManager.Localize("UI_LOADING_STATES");
            }
            else
            {
                button.gameObject.SetActive(false);
                lockedObject.SetActive(true);
                lockedText.text = L10nManager.Localize("UI_INFORM_UNLOCK_RECIPE");
            }
        }

        private void UpdateButtonForConsumable()
        {
            var submittable = CheckNCGAndSlotIsEnough();
            var inventory = States.Instance.CurrentAvatarState.inventory;
            foreach (var material in _selectedRecipeInfo.Materials)
            {
                if (!TableSheets.Instance.MaterialItemSheet.TryGetValue(material.Key, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(row.ItemId, out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                // consumable materials are basically unreplaceable so unreplaceable check isn't required
                if (material.Value > itemCount)
                {
                    submittable = false;
                }
            }

            var cost = new ConditionalCostButton.CostParam(
                CostType.NCG,
                (long)_selectedRecipeInfo.CostNCG);

            button.SetCost(cost);
            button.Interactable = submittable;
            button.gameObject.SetActive(true);
            lockedObject.SetActive(false);
        }

        private void UpdateButtonForEventMaterial()
        {
            materialSelectButton.SetCost(new ConditionalCostButton.CostParam(CostType.NCG, 0));
            materialSelectButton.Interactable = true;
            materialSelectButton.gameObject.SetActive(true);
        }

        public void CombineCurrentRecipe()
        {
            CombinationActionSubject.OnNext(_selectedRecipeInfo);
        }

        private bool CheckNCGAndSlotIsEnough()
        {
            if (_selectedRecipeInfo.CostNCG > States.Instance.GoldBalanceState.Gold.MajorUnit)
            {
                return false;
            }

            var slots = Widget.Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var _))
            {
                return false;
            }

            return true;
        }

        public bool CheckSubmittable(out string errorMessage, out int slotIndex, bool checkSlot = true)
        {
            slotIndex = -1;

            var inventory = States.Instance.CurrentAvatarState.inventory;
            foreach (var material in _selectedRecipeInfo.Materials)
            {
                if (!TableSheets.Instance.MaterialItemSheet.TryGetValue(material.Key, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(row.ItemId, out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                // when a material is unreplaceable.
                if (material.Value > itemCount &&
                    !TableSheets.Instance.CrystalMaterialCostSheet.ContainsKey(material.Key))
                {
                    var message = L10nManager.Localize("UI_UPREPLACEABLE_MATERIAL_FORMAT", row.GetLocalizedName());
                    errorMessage = message;
                    return false;
                }
            }

            if (States.Instance.AgentState is null)
            {
                errorMessage = L10nManager.Localize("FAILED_TO_GET_AGENTSTATE");
                return false;
            }

            if (States.Instance.CurrentAvatarState is null)
            {
                errorMessage = L10nManager.Localize("FAILED_TO_GET_AVATARSTATE");
                return false;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < _selectedRecipeInfo.CostNCG)
            {
                errorMessage = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                return false;
            }

            if (ReactiveAvatarState.ActionPoint < _selectedRecipeInfo.CostAP)
            {
                errorMessage = L10nManager.Localize("ERROR_ACTION_POINT");
                return false;
            }

            if (checkSlot)
            {
                var slots = Widget.Find<CombinationSlotsPopup>();
                if (!slots.TryGetEmptyCombinationSlot(out slotIndex))
                {
                    var message = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_SLOTS");
                    errorMessage = message;
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool CheckCraftableSubRecipe(
            EquipmentItemRecipeSheet.Row equipmentRow, int index)
        {
            var subRecipeIds = equipmentRow.SubRecipeIds;
            if (subRecipeIds == null || !subRecipeIds.Any())
            {
                return false;
            }

            var subRecipe = TableSheets.Instance.EquipmentItemSubRecipeSheetV2[subRecipeIds[index]];
            var isUnlocked = Craft.SharedModel.UnlockedRecipes.Value.Contains(equipmentRow.Id) ||
                             TableSheets.Instance.EquipmentItemRecipeSheet[equipmentRow.Id].CRYSTAL == 0;
            if (!isUnlocked)
            {
                return false;
            }

            var slots = Widget.Find<CombinationSlotsPopup>();
            var isSlotEnough = slots.TryGetEmptyCombinationSlot(out var _);
            if (!isSlotEnough)
            {
                return false;
            }

            var materialMap = new Dictionary<int, int>();
            materialMap.Add(equipmentRow.MaterialId, equipmentRow.MaterialCount);
            foreach (var material in subRecipe.Materials)
            {
                materialMap.Add(material.Id, material.Count);
            }

            var replacedMaterials = GetReplacedMaterials(materialMap);
            var sheet = TableSheets.Instance.CrystalMaterialCostSheet;
            var costCrystal = 0 * CrystalCalculator.CRYSTAL;
            var replaceableMaterials = true;
            foreach (var pair in replacedMaterials)
            {
                try
                {
                    costCrystal +=
                        CrystalCalculator.CalculateMaterialCost(pair.Key, pair.Value, sheet);
                }
                catch (ArgumentException)
                {
                    replaceableMaterials = false;
                    continue;
                }
            }

            var costNCG = equipmentRow.RequiredGold + subRecipe.RequiredGold;
            var isCostEnough =
                States.Instance.GoldBalanceState.Gold.MajorUnit >= costNCG &&
                States.Instance.CrystalBalance.MajorUnit >= costCrystal.MajorUnit &&
                replaceableMaterials;

            return isCostEnough;
        }
    }
}
