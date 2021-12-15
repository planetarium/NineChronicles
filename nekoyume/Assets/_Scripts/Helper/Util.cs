using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Nekoyume.Model.Item;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.Helper
{
    public static class Util
    {
        public const int VisibleEnhancementEffectLevel = 10;
        private const int BlockPerSecond = 12;
        private const string StoredSlotIndex = "AutoSelectedSlotIndex_";

        private static readonly ConcurrentDictionary<Guid, Order> Orders = new ConcurrentDictionary<Guid, Order>();
        private static readonly ConcurrentDictionary<Guid, ItemBase> ItemBases = new ConcurrentDictionary<Guid, ItemBase>();

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

        public static async Task<Order> GetOrder(Guid orderId)
        {
            if (Orders.ContainsKey(orderId))
            {
                return Orders[orderId];
            }

            var address = Order.DeriveAddress(orderId);
            return await UniTask.Run(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(address);
                if (!(state is Dictionary dictionary))
                {
                    return null;
                }

                var order = OrderFactory.Deserialize(dictionary);
                Orders.GetOrAdd(orderId, order);
                return order;

            });
        }

        public static async Task<ItemBase> GetItemBaseByOrderId(Guid orderId)
        {
            // if (ItemBases.ContainsKey(orderId))
            // {
            //     return ItemBases[orderId];
            // }

            var order = await GetOrder(orderId);
            if (order == null)
            {
                return null;
            }

            var address = Addresses.GetItemAddress(order.TradableId);
            return await UniTask.Run(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(address);
                if (!(state is Dictionary dictionary))
                {
                    return null;
                }

                var itemBase = ItemFactory.Deserialize(dictionary);
                ItemBases.GetOrAdd(orderId, itemBase);
                // if (!ItemBases.ContainsKey(orderId))
                // {
                //     ItemBases.Add(orderId, itemBase);
                // }
                return itemBase;
            });
        }

        public static async Task<List<ItemBase>> GetTradableItems(IEnumerable<ShopItem> items)
        {
            var addressList = items.Select(shopItem => shopItem.TradableId.Value)
                .Select(Addresses.GetItemAddress)
                .ToList();
            var values = await Game.Game.instance.Agent.GetStateBulk(addressList);
            var itemBases = new List<ItemBase>();
            foreach (var kv in values)
            {
                if (kv.Value is Bencodex.Types.Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    itemBases.Add(itemBase);
                }
            }

            return itemBases;
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

        public static bool TryGetStoredAvatarSlotIndex(out int slotIndex)
        {
            if (Game.Game.instance.Agent is null)
            {
                Debug.LogError("[Util.TryGetStoredSlotIndex] agent is null");
                slotIndex = 0;
                return false;
            }

            var agentAddress = Game.Game.instance.Agent.Address;
            var key = $"{StoredSlotIndex}{agentAddress}";
            var hasKey = PlayerPrefs.HasKey(key);
            slotIndex = hasKey ? PlayerPrefs.GetInt(key) : 0;
            return hasKey;
        }

        public static void SaveAvatarSlotIndex(int slotIndex)
        {
            if (Game.Game.instance.Agent is null)
            {
                Debug.LogError("[Util.SaveSlotIndex] agent is null");
                return;
            }

            var agentAddress = Game.Game.instance.Agent.Address;
            var key = $"{StoredSlotIndex}{agentAddress}";
            PlayerPrefs.SetInt(key, slotIndex);
        }
    }
}
