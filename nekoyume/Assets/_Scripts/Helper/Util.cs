using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Lib9c.Model.Order;
using Nekoyume.Model.Item;
using Nekoyume.UI;

namespace Nekoyume.Helper
{
    public static class Util
    {
        public const int VisibleEnhancementEffectLevel = 10;
        private const int BlockPerSecond = 12;

        public static string GetBlockToTime(int block)
        {
            var remainSecond = block * BlockPerSecond;
            var timeSpan = TimeSpan.FromSeconds(remainSecond);

            var sb = new StringBuilder();

            if (timeSpan.Days > 0)
            {
                sb.Append($"{timeSpan.Days}d");
            }

            if (timeSpan.Hours > 0)
            {
                if (timeSpan.Days > 0)
                {
                    sb.Append(" ");
                }

                sb.Append($"{timeSpan.Hours}h");
            }

            if (timeSpan.Minutes > 0)
            {
                if (timeSpan.Hours > 0)
                {
                    sb.Append(" ");
                }

                sb.Append($"{timeSpan.Minutes}m");
            }

            if (sb.Length == 0)
            {
                sb.Append("1m");
            }

            return sb.ToString();
        }

        public static Order GetOrder(Guid orderId)
        {
            var address = Order.DeriveAddress(orderId);
            var state = Game.Game.instance.Agent.GetState(address);
            if (state is Dictionary dictionary)
            {
                return OrderFactory.Deserialize(dictionary);
            }

            return null;
        }

        public static string GetItemNameByOrdierId(Guid orderId, bool isNonColored = false)
        {
            var order = GetOrder(orderId);
            if (order == null)
            {
                return string.Empty;
            }

            var address = Addresses.GetItemAddress(order.TradableId);
            var state = Game.Game.instance.Agent.GetState(address);
            if (state is Dictionary dictionary)
            {
                var itemBase = ItemFactory.Deserialize(dictionary);
                return isNonColored ? itemBase.GetLocalizedNonColoredName() : itemBase.GetLocalizedName();
            }

            return string.Empty;
        }

        public static ItemBase GetItemBaseByTradableId(Guid tradableId, long requiredBlockExpiredIndex)
        {
            var address = Addresses.GetItemAddress(tradableId);
            var state = Game.Game.instance.Agent.GetState(address);
            if (state is Dictionary dictionary)
            {
                var itemBase = ItemFactory.Deserialize(dictionary);
                var tradableItem = itemBase as ITradableItem;
                tradableItem.RequiredBlockIndex = requiredBlockExpiredIndex;
                return tradableItem as ItemBase;
            }

            return null;
        }

        public static ItemBase CreateItemBaseByItemId(int itemId)
        {
            var row = Game.Game.instance.TableSheets.ItemSheet[itemId];
            var item = ItemFactory.CreateItem(row, new Cheat.DebugRandom());
            return item;
        }

        public static int GetHourglassCount(Inventory inventory, long currentBlockIndex)
        {
            if (inventory is null)
            {
                return 0;
            }

            var count = 0;
            var materials =
                inventory.Items.OrderByDescending(x => x.item.ItemType == ItemType.Material);
            var hourglass = materials.Where(x => x.item.ItemSubType == ItemSubType.Hourglass);
            foreach (var item in hourglass)
            {
                if (item.item is TradableMaterial tradableItem)
                {
                    if (tradableItem.RequiredBlockIndex > currentBlockIndex)
                    {
                        continue;
                    }
                }

                count += item.count;
            }

            return count;
        }
    }
}
