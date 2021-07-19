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

        public static bool TryGetOptionTagText(this ItemBase itemBase, out string text)
        {
            text = string.Empty;

            if (!(itemBase is Equipment equipment))
            {
                return false;
            }

            var optionCount = equipment.GetOptionCount();
            if (optionCount <= 0)
            {
                return false;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < optionCount; ++i)
            {
                sb.AppendLine("<sprite name=UI_icon_option>");
            }
            text = sb.ToString();
            return true;
        }

        public static string GetCPText(this Equipment equipment)
        {
            var cp = CPHelper.GetCP(equipment);
            return $"<size=80%>CP</size> {cp}";
        }

        public static string GetCPText(this Costume costume, CostumeStatSheet sheet)
        {
            var cp = CPHelper.GetCP(costume, sheet);
            return $"<size=80%>CP</size> {cp}";
        }
    }
}
