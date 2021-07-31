using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RecipeModel
    {
        public readonly Dictionary<
            ItemSubType,
            Dictionary<string, RecipeRow.Model>> EquipmentRecipeMap
                = new Dictionary<
                    ItemSubType,
                    Dictionary<string, RecipeRow.Model>>();

        public readonly Dictionary<
            StatType,
            Dictionary<int, RecipeRow.Model>> ConsumableRecipeMap
                = new Dictionary<
                    StatType,
                    Dictionary<int, RecipeRow.Model>>();

        private const string EquipmentSplitFormat = "{0}_{1}";

        public RecipeModel(
            IEnumerable<EquipmentItemSheet.Row> equipments,
            IEnumerable<RecipeGroup> consumableGroups)
        {
            LoadEquipment(equipments);
            LoadConsumable(consumableGroups);
        }

        private void LoadEquipment(IEnumerable<EquipmentItemSheet.Row> equipments)
        {
            if (equipments is null)
            {
                Debug.LogError("Failed to load equipment recipe.");
                return;
            }

            foreach (var equipment in equipments)
            {
                var itemSubType = equipment.ItemSubType;
                if (!EquipmentRecipeMap.ContainsKey(itemSubType))
                {
                    EquipmentRecipeMap[itemSubType] = new Dictionary<string, RecipeRow.Model>();
                }

                var idString = equipment.Id.ToString();
                var tierArea = idString.Substring(0, 4);
                var variationArea = idString.Substring(5);
                var key = string.Format(EquipmentSplitFormat, tierArea, variationArea);

                if (!EquipmentRecipeMap[itemSubType].TryGetValue(key, out var recipeViewModel))
                {
                    recipeViewModel = new RecipeRow.Model(
                        equipment.GetLocalizedName(),
                        equipment.Grade
                        );
                    EquipmentRecipeMap[itemSubType][key] = recipeViewModel;
                }

                recipeViewModel.Rows.Add(equipment);
            }
        }

        private void LoadConsumable(IEnumerable<RecipeGroup> groups)
        {
            if (groups is null)
            {
                Debug.LogError("Failed to load consumable recipe.");
                return;
            }

            var tableSheets = Game.Game.instance.TableSheets;
            var consumableRecipeSheet = tableSheets.ConsumableItemRecipeSheet;

            foreach (var group in groups)
            {
                foreach (var recipeId in group.RecipeIds)
                {
                    var recipe = consumableRecipeSheet[recipeId];
                    var consumable = tableSheets.ConsumableItemSheet[recipe.ResultConsumableItemId];
                    var statType = consumable.Stats.Any() ? consumable.Stats[0].StatType : StatType.NONE;

                    if (!ConsumableRecipeMap.TryGetValue(statType, out var groupMap))
                    {
                        groupMap = new Dictionary<int, RecipeRow.Model>();
                        ConsumableRecipeMap[statType] = groupMap;
                    }

                    var key = group.Key;
                    if (!groupMap.TryGetValue(key, out var model))
                    {
                        var name = L10nManager.Localize($"ITEM_GROUPNAME_{consumable.Id}");
                        model = new RecipeRow.Model(name, consumable.Grade);
                        groupMap[key] = model;
                    }

                    model.Rows.Add(consumable);
                }
            }
        }
    }
}
