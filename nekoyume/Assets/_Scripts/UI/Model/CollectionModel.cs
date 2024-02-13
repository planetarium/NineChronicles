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

        public bool Active { get; private set; }
        public List<CollectionMaterial> Materials { get; }

        public bool CanActivate => Materials.All(material => material.Enough);

        public CollectionModel(
            CollectionSheet.Row row,
            ItemType itemType,
            bool active,
            List<CollectionMaterial> materials)
        {
            Row = row;
            ItemType = itemType;
            Active = active;
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
                    var collectionMaterial =
                        new CollectionMaterial(material, itemRow.Grade, itemRow.ItemType, active);
                    if (canActive)
                    {
                        collectionMaterial.SetCondition(inventory);
                        canActive &= collectionMaterial.Enough;
                    }

                    materials.Add(collectionMaterial);
                }

                models.Add(new CollectionModel(row, itemType, active, materials));
            }

            return models;
        }

        public static List<StatModifier> GetEffect()
        {
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;
            var data = collectionSheet.Values
                .Where(row => collectionState.Ids.Contains(row.Id))
                .SelectMany(row => row.StatModifiers);

            return data.ToList();
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
