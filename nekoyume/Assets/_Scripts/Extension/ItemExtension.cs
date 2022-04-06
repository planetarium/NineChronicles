using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume
{
    public static class ItemExtension
    {
        public static Sprite GetIconSprite(this ItemBase item) => SpriteHelper.GetItemIcon(item.Id);

        public static Sprite GetBackgroundSprite(this ItemBase item) => SpriteHelper.GetItemBackground(item.Grade);

        public static bool TryParseAsTradableId(this int rowId, ItemSheet itemSheet, out Guid tradableId)
        {
            var itemRow = itemSheet.OrderedList.FirstOrDefault(e => e.Id == rowId);
            if (itemRow is null ||
                !(itemRow is MaterialItemSheet.Row materialRow))
            {
                return false;
            }

            tradableId = TradableMaterial.DeriveTradableId(materialRow.ItemId);
            return true;
        }

        public static bool TryGetFungibleId(this int rowId, ItemSheet itemSheet, out HashDigest<SHA256> fungibleId)
        {
            var itemRow = itemSheet.OrderedList.FirstOrDefault(e => e.Id == rowId);
            if (itemRow is null ||
                !(itemRow is MaterialItemSheet.Row materialRow))
            {
                return false;
            }

            fungibleId = materialRow.ItemId;
            return true;
        }

        public static string GetCPText(this ItemUsable itemUsable)
        {
            var cp = CPHelper.GetCP(itemUsable);
            return $"CP {cp}";
        }

        public static string GetCPText(this Costume costume, CostumeStatSheet sheet)
        {
            var cp = CPHelper.GetCP(costume, sheet);
            return $"CP {cp}";
        }

        public static bool TryGetOptionInfo(this ItemUsable itemUsable, out ItemOptionInfo itemOptionInfo) =>
            ItemOptionHelper.TryGet(itemUsable, out itemOptionInfo);

        public static bool HasElementType(this ItemType type) => type == ItemType.Costume ||
                                                                 type == ItemType.Equipment;

        public static int GetOptionCountFromCombination(this Equipment equipment)
        {
            var additionalStats = equipment.StatsMap.GetAdditionalStats(true).ToList();

            return equipment.optionCountFromCombination > 0
                ? equipment.optionCountFromCombination
                : additionalStats.Count + equipment.Skills.Count;
        }
    }
}
