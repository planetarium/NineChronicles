using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.UI.Model
{
    public class CollectionModel
    {
        public CollectionSheet.Row Row { get; }
        public ItemType ItemType { get; }

        public bool Active { get; private set; } // todo : rx
        public bool CanActivate { get; private set; } // todo : rx
        public List<CollectionMaterial> Materials { get; }

        public CollectionModel(
            CollectionSheet.Row row,
            ItemType itemType,
            bool active,
            bool canActivate,
            List<CollectionMaterial> materials)
        {
            Row = row;
            ItemType = itemType;
            Active = active;
            CanActivate = canActivate;
            Materials = materials;
        }

        // Todo : 캐싱하고 변하는 값만 rx 붙이기
        public static List<CollectionModel> GetModels()
        {
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;

            var models = new List<CollectionModel>();
            foreach (var row in collectionSheet.Values)
            {
                var itemType = itemSheet[row.Materials.First().ItemId].ItemType;
                var active = collectionState.Ids.Contains(row.Id);

                var canActive = !active;
                var materials = new List<CollectionMaterial>();
                foreach (var material in row.Materials)
                {
                    var itemRow = itemSheet[material.ItemId];

                    CollectionMaterial collectionMaterial;
                    if (active)
                    {
                        collectionMaterial = new CollectionMaterial(
                            material, itemRow.Grade, itemRow.ItemType);
                    }
                    else
                    {
                        var items = inventory.Items
                            .Where(item => item.item.Id == material.ItemId).ToArray();

                        var hasItem = items.Any();
                        bool enoughCount;
                        switch (itemRow.ItemType)
                        {
                            case ItemType.Equipment:
                                enoughCount = items
                                    .Select(item => item.item).OfType<Equipment>()
                                    .Any(item => item.level == material.Level &&
                                                 (item.Skills.Any() || !material.SkillContains));
                                break;
                            case ItemType.Material:
                                enoughCount = items.Sum(item => item.count) >= material.Count;
                                break;
                            case ItemType.Consumable:
                                enoughCount = items.Length >= material.Count;
                                break;
                            default:
                                enoughCount = hasItem;
                                break;
                        }

                        collectionMaterial = new CollectionMaterial(
                            material, itemRow.Grade, itemRow.ItemType, hasItem, enoughCount);
                        canActive &= hasItem && enoughCount;
                    }

                    materials.Add(collectionMaterial);
                }

                models.Add(new CollectionModel(row, itemType, active, canActive, materials));
            }

            return models;
        }
    }

    public static class CollectionModelExtension
    {
        public static List<CollectionModel> Sort(
            this IEnumerable<CollectionModel> models,
            ItemType itemType, StatType statType)
        {
            return models
                .Where(model =>
                    model.ItemType == itemType &&
                    model.Row.StatModifiers.Any(stat => statType == StatType.NONE || stat.StatType == statType))
                .OrderByDescending(model => model.CanActivate)
                .ToList();
        }
    }
}
