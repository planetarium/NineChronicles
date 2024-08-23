using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class InventoryExtensions
    {
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

        public static int GetUsableItemCount(this Inventory inventory, int id, long blockIndex)
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

        public static int GetUsableItemCount(this Inventory inventory, CostType type, long blockIndex)
        {
            if ((int)type > 100000)
            {
                return GetUsableItemCount(inventory, (int)type, blockIndex);
            }

            if (type == CostType.Hourglass)
            {
                return GetUsableItemCount(inventory, 400000, blockIndex);
            }

            return 0;
        }

        public static bool HasNotification(
            this Inventory inventory,
            int level,
            long blockIndex,
            ItemRequirementSheet requirementSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet,
            GameConfigState gameConfigState)
        {
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level, gameConfigState);

            foreach (var (type, slotCount) in availableSlots)
            {
                var equipments = inventory.Equipments
                    .Where(e =>
                        e.ItemSubType == type &&
                        e.RequiredBlockIndex <= blockIndex)
                    .ToList();
                var current = equipments.Where(e => e.equipped).ToList();
                // When an equipment slot is empty.
                if (current.Count < Math.Min(equipments.Count, slotCount))
                {
                    return true;
                }

                // When any other equipments are stronger than current one.
                foreach (var equipment in equipments)
                {
                    if (equipment.equipped)
                    {
                        continue;
                    }

                    var cp = CPHelper.GetCP(equipment);
                    foreach (var i in current)
                    {
                        var requirementLevel = i.GetRequirementLevel(
                            requirementSheet,
                            recipeSheet,
                            subRecipeSheet,
                            itemOptionSheet);

                        if (level >= requirementLevel && CPHelper.GetCP(i) < cp)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static readonly int[] DustIds = new[]
        {
            CostType.SilverDust, CostType.GoldDust, CostType.RubyDust, CostType.EmeraldDust
        }.Select(cost => (int)cost).ToArray();

        // Get the priority by Id or ItemSubType of the material group about Inventory sorting.
        // Dust -> ApStone or Hourglass (Tradable -> UnTradable) -> CustomCraft(Drawing) -> Hammer -> Others
        public static int GetMaterialPriority(this Material material)
        {
            if (DustIds.Contains(material.Id))
            {
                return 0;
            }

            if (material.ItemSubType is ItemSubType.ApStone or ItemSubType.Hourglass)
            {
                return 1;
            }

            if (material.ItemSubType is ItemSubType.Drawing or ItemSubType.DrawingTool)
            {
                return 2;
            }

            if (ItemEnhancement.HammerIds.Contains(material.Id))
            {
                return 3;
            }

            return int.MaxValue;
        }
    }
}
