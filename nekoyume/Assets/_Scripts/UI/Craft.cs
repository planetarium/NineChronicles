using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using System.Text.Json;

namespace Nekoyume.UI
{
    using Nekoyume.TableData;
    using UniRx;

    public class Craft : Widget
    {
        [SerializeField] private Toggle equipmentToggle = null;

        [SerializeField] private Toggle consumableToggle = null;

        [SerializeField] private Button closeButton = null;

        [SerializeField] private RecipeScroll recipeScroll = null;

        [SerializeField] private SubRecipeView equipmentSubRecipeView = null;

        [SerializeField] private SubRecipeView foodSubRecipeView = null;

        public static RecipeModel SharedModel = null;

        private const string ConsumableRecipeGroupPath = "Recipe/ConsumableRecipeGroup";

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });

            equipmentToggle.onValueChanged.AddListener(value =>
            {
                if (!value) return;
                AudioController.PlayClick();
                recipeScroll.ShowAsEquipment(ItemSubType.Weapon, true);
            });

            consumableToggle.onValueChanged.AddListener(value =>
            {
                if (!value) return;
                AudioController.PlayClick();
                recipeScroll.ShowAsFood(StatType.HP, true);
            });
        }

        public override void Initialize()
        {
            LoadRecipeModel();
            SharedModel.SelectedRow
                .Subscribe(SetSubRecipe)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (equipmentToggle.isOn)
            {
                recipeScroll.ShowAsEquipment(ItemSubType.Weapon, true);
            }
            else
            {
                equipmentToggle.isOn = true;
            }

            equipmentSubRecipeView.gameObject.SetActive(false);
            foodSubRecipeView.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        private void SetSubRecipe(SheetRow<int> row)
        {
            if (row is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                equipmentSubRecipeView.SetData(equipmentRow, equipmentRow.SubRecipeIds);
                equipmentSubRecipeView.gameObject.SetActive(true);
                foodSubRecipeView.gameObject.SetActive(false);
            }
            else if (row is ConsumableItemRecipeSheet.Row consumableRow)
            {
                foodSubRecipeView.SetData(consumableRow, null);
                equipmentSubRecipeView.gameObject.SetActive(false);
                foodSubRecipeView.gameObject.SetActive(true);
            }
        }

        private void LoadRecipeModel()
        {
            var jsonAsset = Resources.Load<TextAsset>(ConsumableRecipeGroupPath);
            var group = jsonAsset is null ?
                default : JsonSerializer.Deserialize<CombinationRecipeGroup>(jsonAsset.text);

            SharedModel = new RecipeModel(
                Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values,
                group.Groups);
        }
    }
}
