using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RecipeModel
    {
        public readonly Dictionary<string, RecipeRow.Model> EquipmentRecipeMap
                = new Dictionary<string, RecipeRow.Model>();

        public readonly Dictionary<int, RecipeRow.Model> ConsumableRecipeMap
                = new Dictionary<int, RecipeRow.Model>();

        public readonly ReactiveProperty<SheetRow<int>> SelectedRow
            = new ReactiveProperty<SheetRow<int>>();

        private const string EquipmentSplitFormat = "{0}_{1}";

        private RecipeCell _selectedCell = null;

        public RecipeModel(
            IEnumerable<EquipmentItemRecipeSheet.Row> equipments,
            IEnumerable<RecipeGroup> consumableGroups)
        {
            LoadEquipment(equipments);
            LoadConsumable(consumableGroups);
        }

        private void LoadEquipment(IEnumerable<EquipmentItemRecipeSheet.Row> recipes)
        {
            if (recipes is null)
            {
                Debug.LogError("Failed to load equipment recipe.");
                return;
            }

            foreach (var recipe in recipes)
            {
                var idString = recipe.ResultEquipmentId.ToString();
                var tierArea = idString.Substring(0, 4);
                var variationArea = idString.Substring(5);
                var key = string.Format(EquipmentSplitFormat, tierArea, variationArea);

                if (!EquipmentRecipeMap.TryGetValue(key, out var recipeViewModel))
                {
                    var resultItem = recipe.GetResultItem();

                    recipeViewModel = new RecipeRow.Model(
                        resultItem.GetLocalizedName(),
                        resultItem.Grade)
                    {
                        ItemSubType = resultItem.ItemSubType
                    };

                    EquipmentRecipeMap[key] = recipeViewModel;
                }

                recipeViewModel.Rows.Add(recipe);
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
                    var key = group.Key;

                    if (!ConsumableRecipeMap.TryGetValue(key, out var model))
                    {
                        var name = L10nManager.Localize($"ITEM_GROUPNAME_{group.Key}");
                        model = new RecipeRow.Model(name, 0)
                        {
                            StatType = recipe.GetUniqueStat().StatType
                        };
                        ConsumableRecipeMap[key] = model;
                    }
                    model.Rows.Add(recipe);
                }
            }
        }
    }
}
