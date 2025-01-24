using System;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Common;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game;
using Nekoyume.Model.Stat;

namespace Nekoyume.UI
{
    public class SummonDetailPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;

        [SerializeField] private TextMeshProUGUI combatPointText;
        [SerializeField] private TextMeshProUGUI[] mainStatTexts;
        [SerializeField] private RecipeOptionView recipeOptionView;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image[] iconImages;
        [SerializeField] private GameObject spineView;
        [SerializeField] private GameObject iconView;

        [SerializeField]
        private SkillPositionTooltip skillTooltip;

        private const string StatTextFormat = "{0} {1}";

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
            });
            CloseWidget = () =>
            {
                Close(true);
            };
        }

        public void Show(SummonDetailCell.Model model)
        {
            var showCharacterSpine = model.EquipmentRow != null && model.EquipmentRow.ItemSubType == ItemSubType.Aura;
            spineView.SetActive(showCharacterSpine);
            iconView.SetActive(!showCharacterSpine);
            foreach (Transform child in skillTooltip.transform)
            {
                child.gameObject.SetActive(true);
            }

            if (model.EquipmentRow is not null)
            {
                if (model.EquipmentRow.ItemSubType == ItemSubType.Aura)
                {
                    // CharacterView
                    SetCharacter(model.EquipmentRow);
                }

                // MainStatText
                var mainStatText = mainStatTexts[0];
                var stat = model.EquipmentRow.GetUniqueStat();
                var statValueText = stat.StatType.ValueToString((int)stat.TotalValue);
                mainStatText.text = string.Format(StatTextFormat, stat.StatType, statValueText);
                mainStatText.gameObject.SetActive(true);

                // OptionView
                recipeOptionView.SetOptions(model.EquipmentOptions, false, false);

                var optionSheet = TableSheets.Instance.EquipmentItemOptionSheet;
                var skillSheet = TableSheets.Instance.SkillSheet;
                var optionRows = model.EquipmentOptions.Select(info => optionSheet[info.Id]).ToList();

                // SkillTooltip
                SkillSheet.Row skillOption = null;
                var skillOptionRow = optionRows.FirstOrDefault(optionRow =>
                    skillSheet.TryGetValue(optionRow.SkillId, out skillOption));
                if (skillOption != null)
                {
                    skillTooltip.Show(skillOption, skillOptionRow);
                }
                else
                {
                    skillTooltip.gameObject.SetActive(false);
                }

                var minCp = CPHelper.GetStatCP(stat.StatType, stat.BaseValue);
                var maxCp = minCp;
                foreach (var optionRow in optionRows.Where(option => option.StatType != StatType.NONE))
                {
                    minCp += CPHelper.GetStatCP(optionRow.StatType, optionRow.StatMin);
                    maxCp += CPHelper.GetStatCP(optionRow.StatType, optionRow.StatMax);
                }

                var skillCount = optionRows.Count(option => option.StatType == StatType.NONE);
                minCp += CPHelper.GetSkillsMultiplier(skillCount);
                maxCp += CPHelper.GetSkillsMultiplier(skillCount);

                combatPointText.text = $"CP {(int)minCp} - {(int)maxCp}";

                titleText.SetText(model.EquipmentRow.GetLocalizedName(useElementalIcon: false));
                foreach (var iconImage in iconImages)
                {
                    iconImage.sprite = SpriteHelper.GetItemIcon(model.EquipmentRow.Id);
                }
            }

            if (model.CostumeRow is not null)
            {
                spineView.SetActive(true);
                iconView.SetActive(false);
                SetCostume(model.CostumeRow);

                mainStatTexts[0].text = string.Empty;
                var costumeSheet = TableSheets.Instance.CostumeStatSheet;
                titleText.SetText(model.CostumeRow.GetLocalizedName());
                foreach (var iconImage in iconImages)
                {
                    iconImage.sprite = SpriteHelper.GetItemIcon(model.CostumeRow.Id);
                }
                recipeOptionView.SetOptions(model.CostumeStatRows);

                var statsMap = new StatsMap();
                foreach (var r in costumeSheet.OrderedList.Where(r => r.CostumeId == model.CostumeRow.Id))
                {
                    statsMap.AddStatValue(r.StatType, r.Stat);
                }
                var cp = CPHelper.DecimalToInt(CPHelper.GetStatsCP(statsMap));
                combatPointText.text = $"CP {cp}";

                skillTooltip.gameObject.SetActive(true);
                foreach (Transform child in skillTooltip.transform)
                {
                    if (child.name == "Content")
                    {
                        child.gameObject.SetActive(true);
                        child.GetComponent<TextMeshProUGUI>().text = model.CostumeRow.GetLocalizedDescription();
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.RuneTicker))
            {
                mainStatTexts[0].text = string.Empty;
                recipeOptionView.SetOptions(model.RuneOptionInfo);
                if (RuneFrontHelper.TryGetRuneData(model.RuneTicker, out var data))
                {
                    titleText.SetText(LocalizationExtensions.GetLocalizedFavName(data.ticker));
                    foreach (var iconImage in iconImages)
                    {
                        iconImage.sprite = data.icon;
                    }
                }

                if (TableSheets.Instance.SkillSheet.TryGetValue(model.RuneOptionInfo.SkillId,
                    out var skillOption))
                {
                    skillTooltip.Show(skillOption, model.RuneOptionInfo);
                }
                else
                {
                    skillTooltip.gameObject.SetActive(false);
                }
            }

            base.Show();
        }

        private static void SetCostume(CostumeItemSheet.Row costumeRow)
        {
            var game = Game.Game.instance;
            var (equipments, costumes) = game.States.GetEquippedItems(BattleType.Adventure);
            costumes.RemoveAll(costume => costume.ItemSubType == ItemSubType.FullCostume);
            var resultItem = ItemFactory.CreateCostume(costumeRow, Guid.NewGuid());
            costumes.Add(resultItem);
            var avatarState = game.States.CurrentAvatarState;
            game.Lobby.FriendCharacter.Set(avatarState, costumes, equipments);
        }

        private static void SetCharacter(EquipmentItemSheet.Row equipmentRow)
        {
            var game = Game.Game.instance;
            var (equipments, costumes) = game.States.GetEquippedItems(BattleType.Adventure);

            if (equipmentRow is not null)
            {
                var maxLevel = game.TableSheets.EnhancementCostSheetV3.Values
                    .Where(row =>
                        row.ItemSubType == equipmentRow.ItemSubType &&
                        row.Grade == equipmentRow.Grade)
                    .Max(row => row.Level);
                var resultItem = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentRow, Guid.NewGuid(), 0L, maxLevel);

                var sameType = equipments.FirstOrDefault(e => e.ItemSubType == equipmentRow.ItemSubType);
                equipments.Remove(sameType);
                equipments.Add(resultItem);
            }

            var avatarState = game.States.CurrentAvatarState;
            game.Lobby.FriendCharacter.Set(avatarState, costumes, equipments);
        }
    }
}
