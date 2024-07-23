using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class SummonDetailPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [SerializeField] private TextMeshProUGUI[] mainStatTexts;
        [SerializeField] private RecipeOptionView recipeOptionView;

        [SerializeField] private SummonDetailScroll scroll;

        private readonly List<IDisposable> _disposables = new();

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

        public void Show(SummonSheet.Row summonRow)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var runeSheet = tableSheets.RuneSheet;
            var equipmentItemSubRecipeSheet = tableSheets.EquipmentItemSubRecipeSheetV2;

            float ratioSum = summonRow.Recipes.Sum(pair => pair.Item2);
            var models = summonRow.Recipes.Where(pair => pair.Item1 > 0).Select(pair =>
            {
                var (recipeId, ratio) = pair;

                EquipmentItemSheet.Row equipmentRow = null;
                List<EquipmentItemSubRecipeSheetV2.OptionInfo> equipmentOptions = null;
                if (equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
                {
                    equipmentRow = equipmentItemSheet[recipeRow.ResultEquipmentId];
                    equipmentOptions = equipmentItemSubRecipeSheet[recipeRow.SubRecipeIds[0]].Options;
                }

                string runeTicker = null;
                RuneOptionSheet.Row.RuneOptionInfo runeOptionInfo = null;
                if (runeSheet.TryGetValue(recipeId, out var runeRow))
                {
                    runeTicker = runeRow.Ticker;

                    var runeOptionSheet = tableSheets.RuneOptionSheet;
                    if (runeOptionSheet.TryGetValue(runeRow.Id, out var runeOptionRow))
                    {
                        runeOptionRow.LevelOptionMap.TryGetValue(1, out runeOptionInfo);
                    }
                }

                return new SummonDetailCell.Model
                {
                    EquipmentRow = equipmentRow,
                    EquipmentOptions = equipmentOptions,
                    RuneTicker = runeTicker,
                    RuneOptionInfo = runeOptionInfo,
                    Ratio = ratio / ratioSum
                };
            }).OrderBy(model => model.Ratio);

            _disposables.DisposeAllAndClear();
            scroll.UpdateData(models, true);
            scroll.Selected.Subscribe(PreviewDetail).AddTo(_disposables);

            titleText.text = summonRow.GetLocalizedName();
            base.Show();
        }

        private void PreviewDetail(SummonDetailCell.Model model)
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
                recipeOptionView.SetOptions(model.EquipmentOptions, false);
            }

            if (!string.IsNullOrEmpty(model.RuneTicker))
            {
                mainStatTexts[0].gameObject.SetActive(false);
                recipeOptionView.SetOptions(model.RuneOptionInfo);
            }
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
