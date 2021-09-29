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
using UniRx;
using UnityEngine.UI;
using Nekoyume.Model.Item;
using Libplanet;
using System.Security.Cryptography;
using Nekoyume.Extensions;
using Toggle = Nekoyume.UI.Module.Toggle;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    public class SubRecipeView : MonoBehaviour
    {
        public struct RecipeInfo
        {
            public int RecipeId;
            public int? SubRecipeId;
            public BigInteger CostNCG;
            public int CostAP;
            public List<(HashDigest<SHA256>, int count)> Materials;
        }

        [Serializable]
        private struct OptionView
        {
            public GameObject ParentObject;
            public TextMeshProUGUI OptionText;
            public TextMeshProUGUI PercentageText;
        }

        [SerializeField] private GameObject toggleParent = null;
        [SerializeField] private List<Toggle> categoryToggles = null;
        [SerializeField] private RecipeCell recipeCell = null;
        [SerializeField] private TextMeshProUGUI titleText = null;
        [SerializeField] private TextMeshProUGUI statText = null;

        [SerializeField] private TextMeshProUGUI blockIndexText = null;
        [SerializeField] private TextMeshProUGUI greatSuccessRateText = null;

        [SerializeField] private List<OptionView> optionViews = null;
        [SerializeField] private List<OptionView> skillViews = null;

        [SerializeField] private RequiredItemRecipeView requiredItemRecipeView = null;

        [SerializeField] private Button combineButton = null;
        [SerializeField] private GameObject buttonEnabledObject = null;
        [SerializeField] private TextMeshProUGUI costText = null;
        [SerializeField] private Image buttonDisabledImage = null;

        public readonly Subject<RecipeInfo> CombinationActionSubject = new Subject<RecipeInfo>();

        private SheetRow<int> _recipeRow = null;
        private List<int> _subrecipeIds = null;
        private int _selectedIndex;
        private RecipeInfo _selectedRecipeInfo;

        private const string StatTextFormat = "{0} {1}";
        private const string OptionTextFormat = "{0} +({1}-{2})";

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

            combineButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CombineCurrentRecipe();
            });
        }

        public void SetData(SheetRow<int> recipeRow, List<int> subrecipeIds)
        {
            _recipeRow = recipeRow;
            _subrecipeIds = subrecipeIds;

            string title = null;
            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultEquipmentItemRow();
                title = resultItem.GetLocalizedName(true, false);

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.Type, stat.ValueAsInt);
                recipeCell.Show(equipmentRow, false);

            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultConsumableItemRow();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.StatType, stat.ValueAsInt);
                recipeCell.Show(consumableRow, false);
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

            costText.text = _selectedRecipeInfo.CostNCG.ToString();

            buttonEnabledObject.SetActive(true);
        }

        private void UpdateInformation(int index)
        {
            long blockIndex = 0;
            decimal greatSuccessRate = 0m;
            BigInteger costNCG = 0;
            int costAP = 0;
            int recipeId = 0;
            int? subRecipeId = null;
            List<(HashDigest<SHA256> material, int count)> materialList
                = new List<(HashDigest<SHA256> material, int count)>();

            var equipmentRow = _recipeRow as EquipmentItemRecipeSheet.Row;
            var consumableRow = _recipeRow as ConsumableItemRecipeSheet.Row;
            foreach (var optionView in optionViews)
            {
                optionView.ParentObject.SetActive(false);
            }
            foreach (var skillView in skillViews)
            {
                skillView.ParentObject.SetActive(false);
            }

            if (equipmentRow != null)
            {
                var baseMaterialInfo = new EquipmentItemSubRecipeSheet.MaterialInfo(
                    equipmentRow.MaterialId,
                    equipmentRow.MaterialCount);
                blockIndex = equipmentRow.RequiredBlockIndex;
                costNCG = equipmentRow.RequiredGold;
                costAP = equipmentRow.RequiredActionPoint;
                recipeId = equipmentRow.Id;
                var baseMaterial = CreateMaterial(equipmentRow.MaterialId, equipmentRow.MaterialCount);
                materialList.Add(baseMaterial);

                if (_subrecipeIds != null &&
                    _subrecipeIds.Any())
                {
                    toggleParent.SetActive(true);
                    subRecipeId = _subrecipeIds[index];
                    var subRecipe = Game.Game.instance.TableSheets
                        .EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                    var options = subRecipe.Options;

                    blockIndex += subRecipe.RequiredBlockIndex;
                    greatSuccessRate = options
                        .Select(x => x.Ratio.NormalizeFromTenThousandths())
                        .Aggregate((a, b) => a * b);

                    SetOptions(options);
                    requiredItemRecipeView.SetData(
                        baseMaterialInfo,
                        subRecipe.Materials,
                        true);

                    costNCG += subRecipe.RequiredGold;

                    var subMaterials = subRecipe.Materials
                        .Select(x => CreateMaterial(x.Id, x.Count));
                    materialList.AddRange(subMaterials);
                }
                else
                {
                    toggleParent.SetActive(false);
                    requiredItemRecipeView.SetData(baseMaterialInfo, null, true);
                }
            }
            else if (consumableRow != null)
            {
                blockIndex = consumableRow.RequiredBlockIndex;
                requiredItemRecipeView.SetData(consumableRow.Materials, true);
                costNCG = (BigInteger)consumableRow.RequiredGold;
                costAP = consumableRow.RequiredActionPoint;
                recipeId = consumableRow.Id;

                var materials = consumableRow.Materials
                    .Select(x => CreateMaterial(x.Id, x.Count));
                materialList.AddRange(materials);
            }

            blockIndexText.text = blockIndex.ToString();
            greatSuccessRateText.text = greatSuccessRate == 0m ?
                "-" : greatSuccessRate.ToString("P1");

            var recipeInfo = new RecipeInfo
            {
                CostNCG = costNCG,
                CostAP = costAP,
                RecipeId = recipeId,
                SubRecipeId = subRecipeId,
                Materials = materialList
            };
            _selectedRecipeInfo = recipeInfo;

            var submittable = CheckSubmittable(out _, out _);
            buttonDisabledImage.enabled = !submittable;
        }

        private void SetOptions(
            List<EquipmentItemSubRecipeSheetV2.OptionInfo> optionInfos)
        {
            var tableSheets = Game.Game.instance.TableSheets;
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
                    var statMin = option.StatType == StatType.SPD
                        ? (option.StatMin * 0.01m).ToString(CultureInfo.InvariantCulture)
                        : option.StatMin.ToString();

                    var statMax = option.StatType == StatType.SPD
                        ? (option.StatMax * 0.01m).ToString(CultureInfo.InvariantCulture)
                        : (option.StatMax).ToString(CultureInfo.InvariantCulture);

                    var description = string.Format(OptionTextFormat, option.StatType, statMin, statMax);
                    optionView.OptionText.text = description;
                    optionView.PercentageText.text = (ratio.NormalizeFromTenThousandths()).ToString("P0");
                    optionView.ParentObject.transform.SetSiblingIndex(siblingIndex);
                    optionView.ParentObject.SetActive(true);
                }
                else
                {
                    var skillView = skillViews.First(x => !x.ParentObject.activeSelf);
                    var description = skillSheet.TryGetValue(option.SkillId, out var skillRow) ?
                        skillRow.GetLocalizedName() : string.Empty;
                    skillView.OptionText.text = description;
                    skillView.PercentageText.text = (ratio.NormalizeFromTenThousandths()).ToString("P0");
                    skillView.ParentObject.transform.SetSiblingIndex(siblingIndex);
                    skillView.ParentObject.SetActive(true);
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

        public bool CheckSubmittable(out string errorMessage, out int slotIndex)
        {
            slotIndex = -1;
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
                errorMessage = L10nManager.Localize("UI_NOT_ENOUGH_AP");
                return false;
            }

            if (!CheckMaterial(_selectedRecipeInfo.Materials))
            {
                errorMessage = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_MATERIALS");
                return false;
            }

            var slots = Widget.Find<CombinationSlots>();
            if (!slots.TryGetEmptyCombinationSlot(out slotIndex))
            {
                var message = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_SLOTS");
                errorMessage = message;
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool CheckMaterial(List<(HashDigest<SHA256> material, int count)> materials)
        {
            var inventory = States.Instance.CurrentAvatarState.inventory;

            foreach (var material in materials)
            {
                var itemCount = inventory.TryGetFungibleItems(material.material, out var outFungibleItems)
                            ? outFungibleItems.Sum(e => e.count)
                            : 0;

                if (material.count > itemCount)
                {
                    return false;
                }
            }

            return true;
        }

        private (HashDigest<SHA256>, int) CreateMaterial(int id, int count)
        {
            var row = Game.Game.instance.TableSheets.MaterialItemSheet[id];
            var material = ItemFactory.CreateMaterial(row);
            return (material.FungibleId, count);
        }
    }
}
