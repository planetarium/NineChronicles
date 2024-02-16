using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.UI.Model
{
    public class CollectionModel
    {
        public CollectionSheet.Row Row { get; }
        public ItemType ItemType { get; }

        public bool Active { get; set; }
        public List<CollectionMaterial> Materials { get; }

        public bool CanActivate => Materials.All(material => material.Enough);

        public CollectionModel(
            CollectionSheet.Row row,
            ItemType itemType,
            bool active)
        {
            Row = row;
            ItemType = itemType;
            Active = active;
            Materials = new List<CollectionMaterial>();
        }
    }

    public static class CollectionModelExtension
    {
        public static void GenerateModels(this List<CollectionModel> models)
        {
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var activateCollectionIds = Game.Game.instance.States.CollectionState.Ids;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;

            foreach (var row in collectionSheet.Values)
            {
                var itemType = itemSheet[row.Materials.First().ItemId].ItemType;
                var active = activateCollectionIds.Contains(row.Id);
                var model = new CollectionModel(row, itemType, active);

                foreach (var requiredMaterial in model.Row.Materials)
                {
                    var collectionMaterial = new CollectionMaterial(
                        requiredMaterial,
                        itemSheet[requiredMaterial.ItemId].Grade,
                        model.ItemType,
                        model.Active);

                    model.Materials.Add(collectionMaterial);
                }

                models.Add(model);
            }

            models.UpdateMaterials();
        }

        // Update model.Active, model.Materials => material.Active
        public static void UpdateActive(this List<CollectionModel> models)
        {
            var activateCollectionIds = Game.Game.instance.States.CollectionState.Ids;
            foreach (var model in models.Where(model => !model.Active))
            {
                var active = activateCollectionIds.Contains(model.Row.Id);
                if (active)
                {
                    model.Active = true;
                    foreach (var collectionMaterial in model.Materials)
                    {
                        collectionMaterial.Active = true;
                    }
                }
            }
        }

        // Update materials' condition
        public static void UpdateMaterials(this List<CollectionModel> models)
        {
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            foreach (var model in models.Where(model => !model.Active))
            {
                foreach (var collectionMaterial in model.Materials)
                {
                    collectionMaterial.SetCondition(inventory);
                }
            }
        }
    }
}
