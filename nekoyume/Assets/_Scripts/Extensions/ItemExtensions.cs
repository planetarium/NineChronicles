using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume
{
    public static class ItemExtensions
    {
        public static Sprite GetIconSprite(this ItemBase item) => SpriteHelper.GetItemIcon(item.Id);

        public static Sprite GetBackgroundSprite(this ItemBase item) => SpriteHelper.GetItemBackground(item.Grade);

        public static bool TryParseAsTradableId(this int rowId, ItemSheet itemSheet, out Guid tradableId)
        {
            var itemRow = itemSheet.OrderedList.FirstOrDefault(e => e.Id == rowId);
            if (itemRow is null ||
                !(itemRow is MaterialItemSheet.Row materialRow))
            {
                tradableId = default(Guid);
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
                fungibleId = default(HashDigest<SHA256>);
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

        public static bool HasSkill(this Equipment equipment)
        {
            return equipment.Skills.Any() || equipment.BuffSkills.Any();
        }

        public static int GetOptionCountFromCombination(this Equipment equipment)
        {
            var additionalStats = equipment.StatsMap.GetAdditionalStats(true).ToList();

            return equipment.optionCountFromCombination > 0
                ? equipment.optionCountFromCombination
                : additionalStats.Count + equipment.Skills.Count;
        }

        public static List<CountableItem> ToCountableItems(this IEnumerable<ItemBase> items)
        {
            var result = new List<CountableItem>();
            foreach (var itemBase in items)
            {
                if (itemBase is null)
                {
                    continue;
                }

                if (itemBase is IFungibleItem fi)
                {
                    var ci = result.FirstOrDefault(e =>
                        e.ItemBase.HasValue &&
                        e.ItemBase.Value is IFungibleItem fi2 &&
                        fi2.FungibleId.Equals(fi.FungibleId));
                    if (ci is { })
                    {
                        ci.Count.Value++;
                        continue;
                    }
                }

                result.Add(new CountableItem(itemBase, 1));
            }

            return result;
        }
    }
}
