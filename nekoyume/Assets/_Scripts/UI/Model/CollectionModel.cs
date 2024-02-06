using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.UI.Model
{
    public class CollectionModel
    {
        // Todo : 캐싱하고 변하는 값만 rx 붙이기
        public CollectionSheet.Row Row { get; }
        public ItemType ItemType { get; }
        public CollectionMaterial[] Materials { get; }
        public bool Active { get; } // todo : rx
        public bool CanActive { get; }

        public CollectionModel(CollectionSheet.Row row, ItemType itemType, bool active)
        {
            Row = row;
            ItemType = itemType;
            Active = active;
        }

        public static List<CollectionModel> GetModels()
        {
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var models = new List<CollectionModel>();
            foreach (var row in collectionSheet.Values)
            {
                var itemType = itemSheet[row.Materials.First().ItemId].ItemType;
                var active = collectionState.Ids.Contains(row.Id);
                models.Add(new CollectionModel(row, itemType, active));
            }

            return models;
        }
    }
}
