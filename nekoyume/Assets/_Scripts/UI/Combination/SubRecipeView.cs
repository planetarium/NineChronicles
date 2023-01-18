using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.Helper;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;
using System.Globalization;
using Nekoyume.State;
using System.Numerics;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.TableData.Event;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

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
            public BigInteger CostCrystal;
            public int CostAP;
            public Dictionary<int, int> Materials;
            public Dictionary<int, int> ReplacedMaterials;
        }

        [Serializable]
        private struct OptionView
        {
            public GameObject ParentObject;
            public TextMeshProUGUI OptionText;
            public TextMeshProUGUI PercentageText;
            public Slider PercentageSlider;
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
            public Sprite sprite;
        }

        [SerializeField]
        private GameObject toggleParent;

        [SerializeField]
        private List<Toggle> categoryToggles;

        [SerializeField]
        private RecipeCell recipeCell;

        [SerializeField]
        private TextMeshProUGUI titleText;

        // [SerializeField]
        // private TextMeshProUGUI statText;

        [SerializeField]
        private TextMeshProUGUI[] mainStatTexts;

        [SerializeField]
        private TextMeshProUGUI blockIndexText;

        [SerializeField]
        private TextMeshProUGUI greatSuccessRateText;

        [SerializeField]
        private List<OptionView> optionViews;

        [SerializeField]
        private List<OptionView> skillViews;

        [SerializeField]
        private List<GameObject> optionIcons;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView;

        [SerializeField]
        private ConditionalCostButton button;

        [SerializeField]
        private GameObject lockedObject;

        [SerializeField]
        private TextMeshProUGUI lockedText;

        [SerializeField]
        private HammerPointView hammerPointView;

        [SerializeField]
        private ConditionalCostButton materialSelectButton;

        [SerializeField]
        private List<RequiredNormalItemIcon> requiredNormalItemIcons;

        [SerializeField]
        private Image requiredNormalItemImage;

        public readonly Subject<RecipeInfo> CombinationActionSubject = new Subject<RecipeInfo>();

        private SheetRow<int> _recipeRow;
        private List<int> _subrecipeIds;
        private int _selectedIndex;
        private RecipeInfo _selectedRecipeInfo;

        private const string StatTextFormat = "{0} {1}";
        private const int MimisbrunnrRecipeIndex = 2;
        private IDisposable _disposableForOnDisable;

        private bool _canSuperCraft;
        private EquipmentItemOptionSheet.Row _skillOptionRow;
        private HammerPointState _hammerPointState;

        private void Awake()
        {
            for (int i = 0; i < categoryToggles.Count; ++i)
            {
                var innerIndex = i;
                var toggle = categoryToggles[i];
                toggle.onValueChanged.AddListener(value =>
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
                            OneLineSystem.Push(MailType.System, errorMessage, NotificationCell.NotificationType.Alert);
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
            _recipeRow = recipeRow;
            _subrecipeIds = subRecipeIds;

            string title = null;
            var isEquipment = false;
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
                            var stat = resultItem.GetUniqueStat();
                            var statValueText =
                                    stat.Type == StatType.SPD ||
                                    stat.Type == StatType.DRR ||
                                    stat.Type == StatType.CDMG
                                ? (stat.ValueAsInt * 0.01m).ToString(CultureInfo.InvariantCulture)
                                : stat.ValueAsInt.ToString();
                            mainStatText.text = string.Format(StatTextFormat, stat.Type, statValueText);
                            mainStatText.gameObject.SetActive(true);
                            continue;
                        }

                        mainStatText.gameObject.SetActive(false);
                    }

                    recipeCell.Show(equipmentRow, false);
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
                            var stat = resultItem.Stats[i];
                            var statValueText =
                                    stat.StatType == StatType.SPD ||
                                    stat.StatType == StatType.DRR ||
                                    stat.StatType == StatType.CDMG
                                ? (stat.ValueAsInt * 0.01m).ToString(CultureInfo.InvariantCulture)
                                : stat.ValueAsInt.ToString();
                            mainStatText.text = string.Format(StatTextFormat, stat.StatType, statValueText);
                            mainStatText.gameObject.SetActive(true);
                            continue;
                        }

                        mainStatText.gameObject.SetActive(false);
                    }

                    recipeCell.Show(consumableRow, false);
                    break;
                }
                case EventMaterialItemRecipeSheet.Row materialRow :
                {
                    var resultItem = materialRow.GetResultMaterialItemRow();
                    title = resultItem.GetLocalizedName(false, false);
                    mainStatTexts.First().text = resultItem.GetLocalizedDescription();
                    recipeCell.Show(materialRow, false);
                    break;
                }
            }

            titleText.text = title;

            if (categoryToggles.Any())
            {
                var categoryToggle = categoryToggles[_selectedIndex];
                if (categoryToggle.isOn)
                {
                    ChangeTab(_selectedIndex);
                }
                else
                {
                    categoryToggle.isOn = true;
                }
            }
            else
            {
                ChangeTab(0);
            }

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

        private void UpdateInformation(int index)
        {
            long blockIndex = 0;
            decimal greatSuccessRate = 0m;
            BigInteger costNCG = 0;
            int costAP = 0;
            int recipeId = 0;
            int? subRecipeId = null;
            Dictionary<int, int> materialMap = new Dictionary<int, int>();

            var equipmentRow = _recipeRow as EquipmentItemRecipeSheet.Row;
            var consumableRow = _recipeRow as ConsumableItemRecipeSheet.Row;
            var eventMaterialRow = _recipeRow as EventMaterialItemRecipeSheet.Row;
            foreach (var optionView in optionViews)
            {
                optionView.ParentObject.SetActive(false);
            }

            foreach (var skillView in skillViews)
            {
                skillView.ParentObject.SetActive(false);
            }

            optionIcons.ForEach(obj => obj.SetActive(false));
            if (equipmentRow != null)
            {
                var isLocked = !Craft.SharedModel.UnlockedRecipes.Value.Contains(_recipeRow.Key);
                var baseMaterialInfo = new EquipmentItemSubRecipeSheet.MaterialInfo(
                    equipmentRow.MaterialId,
                    equipmentRow.MaterialCount);
                blockIndex = equipmentRow.RequiredBlockIndex;
                costNCG = equipmentRow.RequiredGold;
                costAP = equipmentRow.RequiredActionPoint;
                recipeId = equipmentRow.Id;

                // Add base material
                materialMap.Add(equipmentRow.MaterialId, equipmentRow.MaterialCount);

                if (_subrecipeIds != null &&
                    _subrecipeIds.Any())
                {
                    toggleParent.SetActive(true);
                    subRecipeId = _subrecipeIds[index];
                    var subRecipe = TableSheets.Instance
                        .EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                    var options = subRecipe.Options;

                    blockIndex += subRecipe.RequiredBlockIndex;
                    greatSuccessRate = options
                        .Select(x => x.Ratio.NormalizeFromTenThousandths())
                        .Aggregate((a, b) => a * b);

                    SetOptions(options);

                    var hammerPointStates = States.Instance.HammerPointStates;
                    var showHammerPoint = hammerPointStates is not null &&
                                          hammerPointStates.TryGetValue(recipeId,
                                              out _hammerPointState) &&
                                          index != MimisbrunnrRecipeIndex;
                    hammerPointView.parentObject.SetActive(showHammerPoint);
                    if (showHammerPoint)
                    {
                        var max = TableSheets.Instance.CrystalHammerPointSheet[recipeId].MaxPoint;
                        var increasePoint = index == 0
                            ? CombinationEquipment.BasicSubRecipeHammerPoint
                            : CombinationEquipment.SpecialSubRecipeHammerPoint;
                        var increasedPoint = Math.Min(_hammerPointState.HammerPoint + increasePoint, max);
                        _canSuperCraft = _hammerPointState.HammerPoint == max;
                        var optionSheet = TableSheets.Instance.EquipmentItemOptionSheet;
                        hammerPointView.nowPoint.maxValue = max;
                        hammerPointView.hammerPointText.text =
                            $"{_hammerPointState.HammerPoint}/{max}";
                        hammerPointView.nowPoint.value = _hammerPointState.HammerPoint;
                        hammerPointView.nowPointImage.fillAmount =
                            _hammerPointState.HammerPoint / (float)max;
                        hammerPointView.increasePointImage.fillAmount =
                            increasedPoint / (float) max;
                        hammerPointView.notEnoughHammerPointObject.SetActive(!_canSuperCraft);
                        hammerPointView.enoughHammerPointObject.SetActive(_canSuperCraft);
                        _skillOptionRow = options
                            .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                            .FirstOrDefault(tuple => tuple.option.SkillId != 0)
                            .option;
                    }

                    var sheet = TableSheets.Instance.ItemRequirementSheet;
                    var resultItemRow = equipmentRow.GetResultEquipmentItemRow();

                    if (!sheet.TryGetValue(resultItemRow.Id, out var row))
                    {
                        levelText.enabled = false;
                    }
                    else
                    {
                        var level = index == MimisbrunnrRecipeIndex ? row.MimisLevel : row.Level;
                        levelText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", level);
                        var hasEnoughLevel = States.Instance.CurrentAvatarState.level >= level;
                        levelText.color = hasEnoughLevel
                            ? Palette.GetColor(EnumType.ColorType.ButtonEnabled)
                            : Palette.GetColor(EnumType.ColorType.TextDenial);

                        levelText.enabled = true;
                    }

                    requiredItemRecipeView.SetData(
                        baseMaterialInfo,
                        subRecipe.Materials,
                        true,
                        isLocked);

                    costNCG += subRecipe.RequiredGold;

                    foreach (var material in subRecipe.Materials)
                    {
                        materialMap.Add(material.Id, material.Count);
                    }
                }
                else
                {
                    toggleParent.SetActive(false);
                    requiredItemRecipeView.SetData(baseMaterialInfo, null, true, isLocked);
                }
            }
            else if (consumableRow != null)
            {
                blockIndex = consumableRow.RequiredBlockIndex;
                requiredItemRecipeView.SetData(consumableRow.Materials, true);
                costNCG = (BigInteger)consumableRow.RequiredGold;
                costAP = consumableRow.RequiredActionPoint;
                recipeId = consumableRow.Id;

                var sheet = TableSheets.Instance.ItemRequirementSheet;
                var resultItemRow = consumableRow.GetResultConsumableItemRow();

                if (!sheet.TryGetValue(resultItemRow.Id, out var row))
                {
                    levelText.enabled = false;
                }
                else
                {
                    levelText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", row.Level);
                    var hasEnoughLevel = States.Instance.CurrentAvatarState.level >= row.Level;
                    levelText.color = hasEnoughLevel
                        ? Palette.GetColor(EnumType.ColorType.ButtonEnabled)
                        : Palette.GetColor(EnumType.ColorType.TextDenial);

                    levelText.enabled = true;
                }

                foreach (var material in consumableRow.Materials)
                {
                    materialMap.Add(material.Id, material.Count);
                }
            }
            else if (eventMaterialRow != null)
            {
                blockIndex = 1;
                requiredItemRecipeView.SetData(
                    eventMaterialRow.RequiredMaterialsId,
                    eventMaterialRow.RequiredMaterialsCount);
                recipeId = eventMaterialRow.Id;

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
            }

            blockIndexText.text = blockIndex.ToString();
            greatSuccessRateText.text =
                greatSuccessRate == 0m ? "-" : greatSuccessRate.ToString("0.0%");

            var recipeInfo = new RecipeInfo
            {
                CostNCG = costNCG,
                CostAP = costAP,
                RecipeId = recipeId,
                SubRecipeId = subRecipeId,
                Materials = materialMap,
                ReplacedMaterials = GetReplacedMaterials(materialMap),
            };
            _selectedRecipeInfo = recipeInfo;

            if (equipmentRow != null)
            {
                UpdateButtonForEquipment();
            }
            else if (consumableRow != null)
            {
                UpdateButtonForConsumable();
            }
        }

        private Dictionary<int, int> GetReplacedMaterials(Dictionary<int, int> required)
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

            if (Craft.SharedModel.UnlockedRecipes.Value.Contains(_selectedRecipeInfo.RecipeId))
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

                _selectedRecipeInfo.CostCrystal = crystalCost.MajorUnit;
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

        private void SetOptions(
            List<EquipmentItemSubRecipeSheetV2.OptionInfo> optionInfos)
        {
            var tableSheets = TableSheets.Instance;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;
            var options = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .ToList();

            var statOptions = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Where(x => x.option.StatType != StatType.NONE)
                .ToList();

            var skillOptions = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Except(statOptions)
                .ToList();

            var siblingIndex = 0;
            foreach (var (ratio, option) in options)
            {
                if (option.StatType != StatType.NONE)
                {
                    var optionView = optionViews.First(x => !x.ParentObject.activeSelf);
                    var normalizedRatio = ratio.NormalizeFromTenThousandths();
                    optionView.OptionText.text = option.OptionRowToString();
                    optionView.PercentageText.text = normalizedRatio.ToString("0%");
                    optionView.PercentageSlider.value = (float) normalizedRatio;
                    optionView.ParentObject.transform.SetSiblingIndex(siblingIndex);
                    optionView.ParentObject.SetActive(true);
                    optionIcons[siblingIndex].SetActive(true);
                }
                else
                {
                    var skillView = skillViews.First(x => !x.ParentObject.activeSelf);
                    var description = skillSheet.TryGetValue(option.SkillId, out var skillRow)
                        ? skillRow.GetLocalizedName()
                        : string.Empty;
                    var normalizedRatio = ratio.NormalizeFromTenThousandths();
                    skillView.OptionText.text = description;
                    skillView.PercentageText.text = normalizedRatio.ToString("0%");
                    skillView.PercentageSlider.value = (float) normalizedRatio;
                    skillView.ParentObject.transform.SetSiblingIndex(siblingIndex);
                    skillView.ParentObject.SetActive(true);
                    optionIcons.Last().SetActive(true);
                }

                ++siblingIndex;
            }
        }

        public void CombineCurrentRecipe()
        {
            var loadingScreen = Widget.Find<CombinationLoadingScreen>();
            if (loadingScreen.isActiveAndEnabled)
            {
                return;
            }

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

            if (States.Instance.CurrentAvatarState.actionPoint < _selectedRecipeInfo.CostAP)
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
    }
}
