using System;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Lib9c.Model.Order;
using Nekoyume.Model.Item;

namespace Nekoyume.Helper
{
    public static class Util
    {
        private const int BlockPerSecond = 15;

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

        public static ItemBase GetItemBaseByOrderId(Guid orderId)
        {
            var order = GetOrder(orderId);
            return GetItemBaseByTradableId(order.TradableId);
        }

        public static ItemBase GetItemBaseByTradableId(Guid tradableId)
        {
            var address = Addresses.GetItemAddress(tradableId);
            var state = Game.Game.instance.Agent.GetState(address);
            if (state is Dictionary dictionary)
            {
                return ItemFactory.Deserialize(dictionary);
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
