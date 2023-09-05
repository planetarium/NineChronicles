using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
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

        public void Show(AuraSummonSheet.Row summonRow)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var equipmentItemSubRecipeSheet = tableSheets.EquipmentItemSubRecipeSheetV2;

            var recipes = new List<(int recipeId, int recipeRatio)>
            {
                (summonRow.Recipe1, summonRow.Recipe1Ratio),
                (summonRow.Recipe2, summonRow.Recipe2Ratio),
                (summonRow.Recipe3, summonRow.Recipe3Ratio),
                (summonRow.Recipe4, summonRow.Recipe4Ratio),
                (summonRow.Recipe5, summonRow.Recipe5Ratio),
                (summonRow.Recipe6, summonRow.Recipe6Ratio)
            };
            float ratioSum = recipes.Sum(pair => pair.recipeRatio);

            var models = recipes.Where(pair => pair.recipeId > 0).Select(pair =>
            {
                var recipeRow = equipmentItemRecipeSheet[pair.recipeId];
                return new SummonDetailCell.Model
                {
                    EquipmentRow = equipmentItemSheet[recipeRow.ResultEquipmentId],
                    Options = equipmentItemSubRecipeSheet[recipeRow.SubRecipeIds[0]].Options,
                    Ratio = pair.recipeRatio / ratioSum,
                };
            }).OrderByDescending(model => model.EquipmentRow.Grade);

            scroll.OnClick.Subscribe(PreviewAura).AddTo(_disposables);
            scroll.UpdateData(models, true);

            titleText.text = summonRow.GetLocalizedName();
            base.Show();
        }

        private void PreviewAura(SummonDetailCell.Model model)
        {
            // CharacterView

            // MainStatText
            var mainStatText = mainStatTexts[0];
            var stat = model.EquipmentRow.GetUniqueStat();
            var statValueText = stat.StatType.ValueToString((int)stat.TotalValue);
            mainStatText.text = string.Format(StatTextFormat, stat.StatType, statValueText);
            mainStatText.gameObject.SetActive(true);

            // OptionView
            recipeOptionView.SetOptions(model.Options, false);
        }
    }
}
