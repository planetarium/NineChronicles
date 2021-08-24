using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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

        public bool HasNotification => !(NotifiedRow.Value is null);

        public RecipeCell SelectedRecipeCell { get; set; }
        public EquipmentItemRecipeSheet.Row RecipeForTutorial { get; private set; }
        public HashSet<int> RecipeVFXSkipList { get; private set; }
        private const string RecipeVFXSkipListKey = "Nekoyume.UI.EquipmentRecipe.FirstEnterRecipeKey_{0}";
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
                foreach (var recipeId in group.RecipeIds)
                {
                    var recipe = consumableRecipeSheet[recipeId];
                    var key = group.Key;

                    if (!ConsumableRecipeMap.TryGetValue(key, out var model))
                    {
                        var name = L10nManager.Localize($"ITEM_GROUPNAME_{group.Key}");
                        model = new RecipeRow.Model(name, group.Grade)
                        {
                            ItemSubType = ItemSubType.Food,
                            StatType = recipe.GetUniqueStat().StatType
                        };
                        ConsumableRecipeMap[key] = model;
                    }
                    model.Rows.Add(recipe);
                }
            }
        }

        public void LoadRecipeVFXSkipList()
        {
            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var key = string.Format(RecipeVFXSkipListKey, addressHex);

            if (!PlayerPrefs.HasKey(key))
            {
                CreateRecipeVFXSkipList();
            }
            else
            {
                var bf = new BinaryFormatter();
                var data = PlayerPrefs.GetString(key);
                var bytes = Convert.FromBase64String(data);

                using (var ms = new MemoryStream(bytes))
                {
                    var obj = bf.Deserialize(ms);

                    if (!(obj is HashSet<int>))
                    {
                        CreateRecipeVFXSkipList();
                    }
                    else
                    {
                        RecipeVFXSkipList = (HashSet<int>) obj;
                    }
                }
            }
        }

        public void CreateRecipeVFXSkipList()
        {
            RecipeVFXSkipList = new HashSet<int>();

            var gameInstance = Game.Game.instance;

            var recipeTable = gameInstance.TableSheets.EquipmentItemRecipeSheet;
            var subRecipeTable = gameInstance.TableSheets.EquipmentItemSubRecipeSheet;
            var worldInfo = gameInstance.States.CurrentAvatarState.worldInformation;

            foreach (var recipe in recipeTable.Values
                .Where(x => worldInfo.IsStageCleared(x.UnlockStage)))
            {
                RecipeVFXSkipList.Add(recipe.Id);
            }

            SaveRecipeVFXSkipList();
        }

        public void SaveRecipeVFXSkipList()
        {

            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var key = string.Format(RecipeVFXSkipListKey, addressHex);

            var data = string.Empty;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, RecipeVFXSkipList);
                var bytes = ms.ToArray();
                data = Convert.ToBase64String(bytes);
            }

            PlayerPrefs.SetString(key, data);
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
