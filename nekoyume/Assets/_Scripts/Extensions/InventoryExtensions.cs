using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.Item;

namespace Nekoyume
{
    public static class InventoryExtensions
    {
        public static int GetEquippedFullCostumeOrArmorId(this Inventory inventory)
        {
            foreach (var costume in inventory.Costumes
                         .Where(e =>
                             e.ItemSubType == ItemSubType.FullCostume &&
                             e.Equipped))
            {
                return costume.Id;
            }

            foreach (var armor in inventory.Equipments
                         .Where(e =>
                             e.ItemSubType == ItemSubType.Armor &&
                             e.Equipped))
            {
                return armor.Id;
            }

            return GameConfig.DefaultAvatarArmorId;
        }

        public static int GetMaterialCount(this Inventory inventory, int id)
        {
            if (inventory is null)
            {
                return 0;
            }

            var materialSheet = TableSheets.Instance.MaterialItemSheet;
            if (!materialSheet.TryGetValue(id, out var row))
            {
                return 0;
            }

            return inventory.TryGetFungibleItems(row.ItemId, out var items)
                ? items.Sum(x => x.count)
                : 0;
        }

        public static int GetUsableMaterialCount(this Inventory inventory, int id, long blockIndex)
        {
            if (inventory is null)
            {
                return 0;
            }

            return inventory.Items.Where(item =>
            {
                if (item.Locked)
                {
                    return false;
                }

                if (item.item.Id != id)
                {
                    return false;
                }

                if (item.item is ITradableItem tradableItem &&
                    tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return false;
                }

                return true;
            }).Sum(item => item.count);
        }
    }
}
