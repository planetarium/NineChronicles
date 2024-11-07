﻿using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SummonDetailPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;

        [SerializeField] private TextMeshProUGUI[] mainStatTexts;
        [SerializeField] private RecipeOptionView recipeOptionView;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image iconImage;

        [SerializeField]
        private Nekoyume.UI.Module.Common.SkillPositionTooltip skillTooltip;

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
            // CharacterView
            SetCharacter(model.EquipmentRow);

            if (model.EquipmentRow is not null)
            {
                // MainStatText
                var mainStatText = mainStatTexts[0];
                var stat = model.EquipmentRow.GetUniqueStat();
                var statValueText = stat.StatType.ValueToString((int)stat.TotalValue);
                mainStatText.text = string.Format(StatTextFormat, stat.StatType, statValueText);
                mainStatText.gameObject.SetActive(true);

                // OptionView
                recipeOptionView.SetOptions(model.EquipmentOptions, false, false);
                titleText.SetText(model.EquipmentRow.GetLocalizedName(useElementalIcon: false));
                iconImage.sprite = SpriteHelper.GetItemIcon(model.EquipmentRow.Id);
                SkillSheet.Row skillOption = null;
                var skillOptionRow = model.EquipmentOptions
                    .Select(info =>
                        Nekoyume.Game.TableSheets.Instance.EquipmentItemOptionSheet[info.Id])
                    .FirstOrDefault(optionRow =>
                        Nekoyume.Game.TableSheets.Instance.SkillSheet.TryGetValue(optionRow.SkillId,
                            out skillOption));

                if (skillOption != null)
                {
                    skillTooltip.Show(skillOption, skillOptionRow);
                }
                else
                {
                    skillTooltip.gameObject.SetActive(false);
                }
            }

            if (!string.IsNullOrEmpty(model.RuneTicker))
            {
                mainStatTexts[0].gameObject.SetActive(false);
                recipeOptionView.SetOptions(model.RuneOptionInfo);
                if (RuneFrontHelper.TryGetRuneData(model.RuneTicker, out var data))
                {
                    titleText.SetText(LocalizationExtensions.GetLocalizedFavName(data.ticker));
                    iconImage.sprite = data.icon;
                }

                if (Nekoyume.Game.TableSheets.Instance.SkillSheet.TryGetValue(model.RuneOptionInfo.SkillId,
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
