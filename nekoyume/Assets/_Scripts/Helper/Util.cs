using System;
using System.Text;
using Bencodex.Types;
using Lib9c.Model.Order;
using Nekoyume.Model.Item;

namespace Nekoyume.Helper
{
    public static class Util
    {
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
    }
}
