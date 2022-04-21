using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
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

        public readonly ReactiveProperty<SheetRow<int>> NotifiedRow
            = new ReactiveProperty<SheetRow<int>>();

        public readonly ReactiveProperty<List<int>> UnlockedRecipes =
            new ReactiveProperty<List<int>>();

        public readonly List<int> UnlockingRecipes = new List<int>();

        public bool HasNotification => !(NotifiedRow.Value is null);
        public RecipeCell SelectedRecipeCell { get; set; }
        public EquipmentItemRecipeSheet.Row RecipeForTutorial { get; private set; }
        private const string EquipmentSplitFormat = "{0}_{1}";
        private const int RecipeIdForTutorial = 1;

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

            RecipeForTutorial = recipes.FirstOrDefault(x => x.Id == RecipeIdForTutorial);
            if (RecipeForTutorial is null)
            {
                Debug.LogError($"Failed to load recipe for tutorial. id : {RecipeIdForTutorial}");
            }

            foreach (var recipe in recipes)
            {
                var key = GetEquipmentGroup(recipe.ResultEquipmentId);

                if (!EquipmentRecipeMap.TryGetValue(key, out var recipeViewModel))
                {
                    var resultItem = recipe.GetResultEquipmentItemRow();

                    recipeViewModel = new RecipeRow.Model(
                        resultItem.GetLocalizedName(false, false),
                        resultItem.Grade)
                    {
                        ItemSubType = resultItem.ItemSubType,
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
                var key = group.Key;
                if (!ConsumableRecipeMap.TryGetValue(key, out var model))
                {
                    var name = L10nManager.Localize($"ITEM_GROUPNAME_{key}");
                    model = new RecipeRow.Model(name, group.Grade)
                    {
                        ItemSubType = ItemSubType.Food,
                    };
                    ConsumableRecipeMap[key] = model;
                }

                var first = group.RecipeIds.FirstOrDefault();
                if (!consumableRecipeSheet.TryGetValue(first, out var firstRecipe))
                {
                    continue;
                }
                model.StatType = firstRecipe.GetUniqueStat().StatType;

                foreach (var recipeId in group.RecipeIds)
                {
                    if (!consumableRecipeSheet.TryGetValue(recipeId, out var recipe))
                    {
                        continue;
                    }

                    model.Rows.Add(recipe);
                }
            }
        }

        public async void UpdateUnlockedRecipesAsync(Address address)
        {
            var unlockedRecipeIdsAddress = address.Derive("recipe_ids");
            var task = Game.Game.instance.Agent.GetStateAsync(unlockedRecipeIdsAddress);
            await task;
            var result = task.Result != null ?
                ((List)task.Result).ToList(StateExtensions.ToInteger) :
                new List<int>() { 1 };
            SetUnlockedRecipes(result);
        }

        public void SetUnlockedRecipes(List<int> recipeIds)
        {
            UnlockedRecipes.SetValueAndForceNotify(recipeIds);
        }

        public static string GetEquipmentGroup(int itemId)
        {
            var idString = itemId.ToString();
            var tierArea = idString.Substring(0, 4);
            var variationArea = idString.Substring(5);
            return string.Format(EquipmentSplitFormat, tierArea, variationArea);
        }
    }
}
