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
using Toggle = Nekoyume.UI.Module.Toggle;
using Material = Nekoyume.Model.Item.Material;

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
        [SerializeField] private GameObject buttonDisabledObject = null;
        [SerializeField] private GameObject lockObject = null;

        public readonly Subject<RecipeInfo> CombinationActionSubject = new Subject<RecipeInfo>();

        private SheetRow<int> _recipeRow = null;
        private List<int> _subrecipeIds = null;
        private int _selectedIndex;
        private RecipeInfo _selectedRecipeInfo;

        private const string StatTextFormat = "{0} {1}";
        private const string OptionTextFormat = "{0} +({1}~{2})";

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
                CombinationActionSubject.OnNext(_selectedRecipeInfo));
        }

        public void SetData(SheetRow<int> recipeRow, List<int> subrecipeIds)
        {
            _recipeRow = recipeRow;
            _subrecipeIds = subrecipeIds;

            string title = null;
            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultItem();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.Type, stat.ValueAsInt);
                recipeCell.Show(equipmentRow, false);

            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultItem();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.StatType, stat.ValueAsInt);
                recipeCell.Show(consumableRow, false);
            }

            titleText.text = title;

            if (categoryToggles.Any())
            {
                var firstCategoryToggle = categoryToggles.First();
                if (firstCategoryToggle.isOn)
                {
                    ChangeTab(0);
                }
                else
                {
                    firstCategoryToggle.isOn = true;
                }
            }
            else
            {
                ChangeTab(0);
            }
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
            combineButton.interactable = CheckSubmittable(_selectedRecipeInfo);

            buttonEnabledObject.SetActive(true);
            buttonDisabledObject.SetActive(false);
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

            if (equipmentRow != null)
            {
                var baseMaterialInfo = new EquipmentItemSubRecipeSheet.MaterialInfo(
                    equipmentRow.MaterialId,
                    equipmentRow.MaterialCount);
                costNCG = equipmentRow.RequiredGold;
                costAP = equipmentRow.RequiredActionPoint;
                recipeId = equipmentRow.Id;
                var baseMaterial = CreateMaterial(equipmentRow.MaterialId, equipmentRow.MaterialCount);
                materialList.Add(baseMaterial);

                if (_subrecipeIds != null &&
                    _subrecipeIds.Any())
                {
                    subRecipeId = _subrecipeIds[index];
                    var subRecipe = Game.Game.instance.TableSheets
                        .EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                    var options = subRecipe.Options;

                    blockIndex = subRecipe.RequiredBlockIndex;
                    greatSuccessRate = options
                        .Select(x => x.Ratio)
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
                    blockIndex = equipmentRow.RequiredBlockIndex;
                    foreach (var optionView in optionViews)
                    {
                        optionView.ParentObject.SetActive(false);
                    }
                    foreach (var skillView in skillViews)
                    {
                        skillView.ParentObject.SetActive(false);
                    }
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
        }

        private void SetOptions(
            List<EquipmentItemSubRecipeSheetV2.OptionInfo> options)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;
            var statOptions = options
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Where(x => x.option.StatType != StatType.NONE)
                .ToList();

            var skillOptions = options
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Except(statOptions)
                .ToList();

            for (int i = 0; i < optionViews.Count; ++i)
            {
                var optionView = optionViews[i];
                if (i >= statOptions.Count)
                {
                    optionView.ParentObject.SetActive(false);
                    continue;
                }

                var option = statOptions[i].option;
                var ratioText = statOptions[i].ratio.ToString("P");
                var statMin = option.StatType == StatType.SPD
                    ? (option.StatMin / 100f).ToString(CultureInfo.InvariantCulture)
                    : option.StatMin.ToString();

                var statMax = option.StatType == StatType.SPD
                    ? (option.StatMax / 100f).ToString(CultureInfo.InvariantCulture)
                    : option.StatMax.ToString();

                var description = string.Format(OptionTextFormat, option.StatType, statMin, statMax);
                optionView.OptionText.text = description;
                optionView.PercentageText.text = ratioText;
                optionView.ParentObject.SetActive(true);
            }

            for (int i = 0; i < skillViews.Count; ++i)
            {
                var skillView = skillViews[i];
                if (i >= skillOptions.Count)
                {
                    skillView.ParentObject.SetActive(false);
                    continue;
                }

                var option = skillOptions[i].option;
                var ratioText = skillOptions[i].ratio.ToString("P");

                var description = skillSheet.TryGetValue(option.SkillId, out var skillRow) ?
                    skillRow.GetLocalizedName() : string.Empty;
                skillView.OptionText.text = description;
                skillView.PercentageText.text = ratioText;
                skillView.ParentObject.SetActive(true);
            }
        }

        private bool CheckSubmittable(RecipeInfo recipeInfo)
        {
            return !(States.Instance.AgentState is null) &&
                States.Instance.GoldBalanceState.Gold.MajorUnit >= recipeInfo.CostNCG &&
                States.Instance.CurrentAvatarState.actionPoint >= recipeInfo.CostAP &&
                CheckMaterial(recipeInfo.Materials) &&
                !(States.Instance.CurrentAvatarState is null);
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
