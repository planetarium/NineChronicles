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
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;
using Nekoyume.State;
using System.Numerics;
using Nekoyume.BlockChain;
using System.Collections;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;

namespace Nekoyume.UI
{
    public class SubRecipeView : MonoBehaviour
    {
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
        [SerializeField] private Button submitButton = null;
        [SerializeField] private GameObject buttonEnabledObject = null;
        [SerializeField] private TextMeshProUGUI costText = null;
        [SerializeField] private GameObject buttonDisabledObject = null;
        [SerializeField] private GameObject lockObject = null;

        private SheetRow<int> _recipeRow = null;
        private List<int> _subrecipeIds = null;
        private int _subrecipeIndex;
        private BigInteger _costNCG;

        private bool IsSubmittable =>
            !(States.Instance.AgentState is null) &&
            States.Instance.GoldBalanceState.Gold.MajorUnit >= _costNCG &&
            !(States.Instance.CurrentAvatarState is null);

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

            submitButton.onClick.AddListener(SubscribeOnClick);
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
                recipeCell.Show(equipmentRow);

            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultItem();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.StatType, stat.ValueAsInt);
                recipeCell.Show(consumableRow);

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

        private void ChangeTab(int index)
        {
            _subrecipeIndex = index;
            long blockIndex = 0;
            decimal greatSuccessRate = 0m;
            decimal costNCG = 0;

            var equipmentRow = _recipeRow as EquipmentItemRecipeSheet.Row;
            var consumableRow = _recipeRow as ConsumableItemRecipeSheet.Row;

            if (equipmentRow != null)
            {
                var baseMaterial = new EquipmentItemSubRecipeSheet.MaterialInfo(
                    equipmentRow.MaterialId,
                    equipmentRow.MaterialCount);
                costNCG = equipmentRow.RequiredGold;

                if (_subrecipeIds != null &&
                    _subrecipeIds.Any())
                {
                    var subRecipeId = _subrecipeIds[index];
                    var subRecipe = Game.Game.instance.TableSheets
                        .EquipmentItemSubRecipeSheetV2[subRecipeId];
                    var options = subRecipe.Options;

                    blockIndex = subRecipe.RequiredBlockIndex;
                    greatSuccessRate = options
                        .Select(x => x.Ratio)
                        .Aggregate((a, b) => a * b);

                    SetOptions(options);
                    requiredItemRecipeView.SetData(
                        baseMaterial,
                        subRecipe.Materials,
                        true);

                    costNCG += subRecipe.RequiredGold;
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
                    requiredItemRecipeView.SetData(
                        baseMaterial,
                        null,
                        true);
                }
            }
            else if (consumableRow != null)
            {
                blockIndex = consumableRow.RequiredBlockIndex;
                requiredItemRecipeView.SetData(consumableRow.Materials, true);
                costNCG = consumableRow.RequiredGold;
            }

            blockIndexText.text = blockIndex.ToString();
            greatSuccessRateText.text = greatSuccessRate == 0m ?
                "-" : greatSuccessRate.ToString("P1");

            _costNCG = (BigInteger) costNCG;
            costText.text = _costNCG.ToString();
            submitButton.interactable =
                States.Instance.GoldBalanceState.Gold.MajorUnit >= _costNCG;

            buttonEnabledObject.SetActive(true);
            buttonDisabledObject.SetActive(false);
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

        private void SubscribeOnClick()
        {
            var slots = Widget.Find<CombinationSlots>();
            if (!slots.TryGetEmptyCombinationSlSlot(out var slotIndex))
            {
                return;
            }

            slots.SetCaching(slotIndex, true);

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -_costNCG);


            if (_recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var subRecipeId = _subrecipeIds.Any() ?
                    _subrecipeIds[_subrecipeIndex] : (int?) null;

                Game.Game.instance.ActionManager.CombinationEquipment(
                    equipmentRow.Id,
                    slotIndex,
                    subRecipeId);

                var equipment = (Equipment) ItemFactory.CreateItemUsable(
                    equipmentRow.GetResultItem(), Guid.Empty, default);
                StartCoroutine(CoCombineNPCAnimation(equipment, null));
            }
            else if (_recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                Game.Game.instance.ActionManager.CombinationConsumable(
                    consumableRow.Id,
                    slotIndex);

                var consumable = (Consumable) ItemFactory.CreateItemUsable(
                    consumableRow.GetResultItem(), Guid.Empty, default);
                StartCoroutine(CoCombineNPCAnimation(consumable, null, true));
            }
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, System.Action action, bool isConsumable = false)
        {
            var loadingScreen = Widget.Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(action);
            //Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.AnimateNPC();
        }
    }
}
