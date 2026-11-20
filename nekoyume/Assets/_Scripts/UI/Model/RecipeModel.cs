using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RecipeModel
    {
        public readonly Dictionary<ItemSubType, List<EquipmentItemRecipeSheet.Row>> EquipmentRecipeMap = new();

        public readonly Dictionary<StatType, List<ConsumableItemRecipeSheet.Row>> ConsumableRecipeMap = new();

        public readonly List<SheetRow<int>> EventConsumableRecipeMap = new();

        public readonly List<SheetRow<int>> EventMaterialRecipeMap = new();

        public readonly ReactiveProperty<SheetRow<int>> SelectedRow = new();

        public readonly ReactiveProperty<SheetRow<int>> NotifiedRow = new();

        public readonly ReactiveProperty<List<int>> UnlockedRecipes = new();

        public readonly ReactiveProperty<List<int>> UnlockableRecipes = new();

        public long UnlockableRecipesOpenCost { get; private set; }
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
                NcDebug.LogError("Failed to load equipment recipe.");
                return;
            }

            RecipeForTutorial = recipes.FirstOrDefault(x => x.Id == RecipeIdForTutorial);
            if (RecipeForTutorial is null)
            {
                NcDebug.LogError($"Failed to load recipe for tutorial. id : {RecipeIdForTutorial}");
            }

            foreach (var recipe in recipes)
            {
                var isEventEquipment = Util.IsEventEquipmentRecipe(recipe.Id);

                ItemSubType itemSubType;
                if (isEventEquipment)
                {
                    itemSubType = ItemSubType.EquipmentMaterial;
                }
                else if (recipe.TryGetResultEquipmentItemRow(out var resultRow))
                {
                    itemSubType = resultRow.ItemSubType;
                }
                else
                {
                    // 결과 장비가 시트에 없는 레시피는 목록에서 뺀다(여기서 던지면 위젯 초기화가 멈춰 부팅이 멈춘다).
                    continue;
                }
                if (!EquipmentRecipeMap.ContainsKey(itemSubType))
                {
                    EquipmentRecipeMap[itemSubType] = new List<EquipmentItemRecipeSheet.Row>();
                }

                EquipmentRecipeMap[itemSubType].Add(recipe);
            }
        }

        private void LoadConsumable(IEnumerable<RecipeGroup> groups)
        {
            if (groups is null)
            {
                NcDebug.LogError("Failed to load consumable recipe.");
                return;
            }

            var consumableRecipeSheet = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet;

            foreach (var group in groups)
            {
                if (!consumableRecipeSheet.TryGetValue(group.Key, out var firstRecipe))
                {
                    continue;
                }

                var resultItem = firstRecipe.GetResultConsumableItemRow();
                var statType = resultItem.GetUniqueStat().StatType;
                if (!ConsumableRecipeMap.ContainsKey(statType))
                {
                    ConsumableRecipeMap[statType] = new List<ConsumableItemRecipeSheet.Row>();
                }

                foreach (var recipeId in group.RecipeIds)
                {
                    if (!consumableRecipeSheet.TryGetValue(recipeId, out var recipe))
                    {
                        continue;
                    }

                    ConsumableRecipeMap[statType].Add(recipe);
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

            foreach (var row in rows)
            {
                EventConsumableRecipeMap.Add(row);
            }
        }

        public void UpdateEventMaterial(List<EventMaterialItemRecipeSheet.Row> rows)
        {
            EventMaterialRecipeMap.Clear();
            if (rows is null)
            {
                return;
            }

            foreach (var row in rows)
            {
                EventMaterialRecipeMap.Add(row);
            }
        }

        public async void UpdateUnlockedRecipesAsync(Address address)
        {
            var unlockedRecipeIdsAddress = address.Derive("recipe_ids");
            var recipeState = await Game.Game.instance.Agent.GetStateAsync(
                ReservedAddresses.LegacyAccount,
                unlockedRecipeIdsAddress);
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
                lastClearedStageId = 1;
            }

            var sheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            // 하위 분류는 레시피 행 값을 먼저 본다 — 체인(UnlockEquipmentRecipe)이 이 값과 UnlockStage 순서로
            // "앞 레시피가 먼저 해금돼야 함"을 검사하므로, 결과 장비가 없어 화면에서 뺀 레시피도 순서 계산에서는 빼면 안 된다.
            var availableRecipes = sheet.Values
                .Where(x =>
                    (x.ItemSubType ?? (x.TryGetResultEquipmentItemRow(out var resultRow)
                        ? resultRow.ItemSubType
                        : (ItemSubType?)null)) == DisplayingItemSubtype &&
                    x.UnlockStage <= lastClearedStageId &&
                    !UnlockedRecipes.Value.Contains(x.Id) &&
                    !UnlockingRecipes.Contains(x.Id) &&
                    x.CRYSTAL != 0)
                .OrderBy(x => x.UnlockStage);

            var unlockableRecipes = new List<int>();
            var balance = States.Instance.CrystalBalance.MajorUnit;
            var totalCost = 0L;
            foreach (var availableRecipe in availableRecipes)
            {
                // 결과 장비가 이 클라 시트에 없는 레시피에서 멈춘다 — 건너뛰고 뒤 레시피를 넣으면 체인이 tx 전체를 거부한다.
                if (!availableRecipe.TryGetResultEquipmentItemRow(out _))
                {
                    break;
                }

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
    }
}
