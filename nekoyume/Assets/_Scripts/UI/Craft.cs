using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using System.Text.Json;

namespace Nekoyume.UI
{
    public class Craft : Widget
    {
        [SerializeField] private Toggle equipmentToggle = null;

        [SerializeField] private Toggle consumableToggle = null;

        [SerializeField] private Button closeButton = null;

        [SerializeField] private RecipeScroll recipeScroll = null;

        public static RecipeModel SharedModel = null;

        private const string ConsumableRecipeGroupPath = "Recipe/ConsumableRecipeGroup";

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close(true));

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
            base.Show(ignoreShowAnimation);
        }

        private void LoadRecipeModel()
        {
            var tableSheets = Game.Game.instance.TableSheets;

            var equipmentRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var equipmentIds = equipmentRecipeSheet.Values
                .Select(r => r.ResultEquipmentId);
            var equipments = tableSheets.EquipmentItemSheet.Values
                .Where(r => equipmentIds.Contains(r.Id));

            var jsonAsset = Resources.Load<TextAsset>(ConsumableRecipeGroupPath);
            var group = jsonAsset is null ?
                default : JsonSerializer.Deserialize<CombinationRecipeGroup>(jsonAsset.text);

            SharedModel = new RecipeModel(equipments, group.Groups);
        }
    }
}
