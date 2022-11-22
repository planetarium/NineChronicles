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
using System.Collections.Generic;
using System.Linq;
using Nekoyume.TableData.Event;
using UniRx;
using UnityEngine;
using StateExtensions = Nekoyume.Model.State.StateExtensions;

namespace Nekoyume.UI.Model
{
    public class RecipeModel
    {
        public readonly Dictionary<string, RecipeRow.Model> EquipmentRecipeMap = new();

        public readonly Dictionary<int, RecipeRow.Model> ConsumableRecipeMap = new();

        public readonly Dictionary<ItemSubType, RecipeRow.Model> EventConsumableRecipeMap = new();

        public readonly Dictionary<int, RecipeRow.Model> EventMaterialRecipeMap = new();

        public readonly ReactiveProperty<SheetRow<int>> SelectedRow = new();

        public readonly ReactiveProperty<SheetRow<int>> NotifiedRow = new();

        public readonly ReactiveProperty<List<int>> UnlockedRecipes = new();

        public readonly ReactiveProperty<List<int>> UnlockableRecipes = new();

        public int UnlockableRecipesOpenCost { get; private set; }
        public ItemSubType DisplayingItemSubtype { get; set; }

        public readonly List<int> UnlockingRecipes = new();
        public readonly List<int> DummyLockedRecipes = new();

        public bool HasNotification => !(NotifiedRow.Value is null);
        public RecipeCell SelectedRecipeCell { get; set; }
        public EquipmentItemRecipeSheet.Row RecipeForTutorial { get; private set; }
        private const string EquipmentSplitFormat = "{0}_{1}";
        private const int RecipeIdForTutorial = 1;

        public RecipeModel(
            IEnumerable<EquipmentItemRecipeSheet.Row> equipments,
            IEnumerable<RecipeGroup> consumableGroups,
            List<EventConsumableItemRecipeSheet.Row> eventConsumables,
            List<EventMaterialItemRecipeSheet.Row> eventMaterials)
        {
            LoadEquipment(equipments);
            LoadConsumable(consumableGroups);
            UpdateEventConsumable(eventConsumables);
            UpdateEventMaterial(eventMaterials);
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

        public void UpdateEventConsumable(List<EventConsumableItemRecipeSheet.Row> rows)
        {
            EventConsumableRecipeMap.Clear();
            if (rows is null)
            {
                return;
            }

            var title = L10nManager.Localize("UI_EVENT_FOOD");
            var value = new RecipeRow.Model(title, 0)
            {
                ItemSubType = ItemSubType.Food,
            };
            foreach (var row in rows)
            {
                value.Rows.Add(row);
            }

            EventConsumableRecipeMap[ItemSubType.Food] = value;
        }

        public void UpdateEventMaterial(List<EventMaterialItemRecipeSheet.Row> rows)
        {
            EventMaterialRecipeMap.Clear();
            if (rows is null)
            {
                return;
            }

            var groups = rows.GroupBy(x => x.ResultMaterialItemId);
            foreach (var group in groups)
            {
                var result = group.First().GetResultMaterialItemRow();
                var model = new RecipeRow.Model(result.GetLocalizedName(false, false), 0)
                {
                    ItemSubType = result.ItemSubType,
                };
                foreach (var row in group)
                {
                    model.Rows.Add(row);
                }

                EventMaterialRecipeMap[group.Key] = model;
            }
        }

        public async void UpdateUnlockedRecipesAsync(Address address)
        {
            var unlockedRecipeIdsAddress = address.Derive("recipe_ids");
            var recipeState = await Game.Game.instance.Agent.GetStateAsync(unlockedRecipeIdsAddress);
            var result = recipeState != null && !(recipeState is Null)
                ? recipeState.ToList(StateExtensions.ToInteger)
                : new List<int> { 1 };
            SetUnlockedRecipes(result);
            States.Instance.UpdateHammerPointStates(result);
        }

        public void SetUnlockedRecipes(List<int> recipeIds)
        {
            UnlockedRecipes.SetValueAndForceNotify(recipeIds);
        }

        public void UpdateUnlockableRecipes()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var lastClearedStageId))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            var availableRecipes = sheet.Values
                .Where(x =>
                    x.GetResultEquipmentItemRow().ItemSubType == DisplayingItemSubtype &&
                    x.UnlockStage <= lastClearedStageId &&
                    !UnlockedRecipes.Value.Contains(x.Id) &&
                    !UnlockingRecipes.Contains(x.Id))
                .OrderBy(x => x.UnlockStage);

            var unlockableRecipes = new List<int>();
            var balance = States.Instance.CrystalBalance.MajorUnit;
            var totalCost = 0;
            foreach (var availableRecipe in availableRecipes)
            {
                // should contain at least one recipe.
                if (unlockableRecipes.Any() && totalCost + availableRecipe.CRYSTAL > balance)
                {
                    break;
                }

                totalCost += availableRecipe.CRYSTAL;
                unlockableRecipes.Add(availableRecipe.Id);
            }

            UnlockableRecipesOpenCost = totalCost;
            UnlockableRecipes.SetValueAndForceNotify(unlockableRecipes);
        }

        public static string GetEquipmentGroup(int itemId)
        {
            var idString = itemId.ToString();
            var tierArea = idString[..4];
            var variationArea = idString[5..];
            return string.Format(EquipmentSplitFormat, tierArea, variationArea);
        }
    }
}
