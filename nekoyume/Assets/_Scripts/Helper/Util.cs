using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Nekoyume.Extensions;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class Util
    {
        public const int VisibleEnhancementEffectLevel = 10;
        private const int BlockPerSecond = 12;
        private const string StoredSlotIndex = "AutoSelectedSlotIndex_";

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
            var address = Order.DeriveAddress(orderId);
            return await UniTask.Run(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(address);
                if (state is Dictionary dictionary)
                {
                    return OrderFactory.Deserialize(dictionary);
                }

                return null;
            });
        }

        public static async Task<string> GetItemNameByOrderId(Guid orderId, bool isNonColored = false)
        {
            var order = await GetOrder(orderId);
            if (order == null)
            {
                return string.Empty;
            }

            var address = Addresses.GetItemAddress(order.TradableId);
            return await UniTask.Run(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(address);
                if (state is Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    return isNonColored
                        ? itemBase.GetLocalizedNonColoredName()
                        : itemBase.GetLocalizedName();
                }

                return string.Empty;
            });
        }

        public static async Task<ItemBase> GetItemBaseByTradableId(Guid tradableId, long requiredBlockExpiredIndex)
        {
            var address = Addresses.GetItemAddress(tradableId);
            return await UniTask.Run(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(address);
                if (state is Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    var tradableItem = itemBase as ITradableItem;
                    tradableItem.RequiredBlockIndex = requiredBlockExpiredIndex;
                    return tradableItem as ItemBase;
                }

                return null;
            });
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

        public static bool IsUsableItem(ItemBase itemBase)
        {
            var sheets = Game.Game.instance.TableSheets;
            var requirementSheet = sheets.ItemRequirementSheet;
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null || itemBase is null)
            {
                return false;
            }

            switch (itemBase.ItemType)
            {
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
                    if (!requirementSheet.TryGetValue(itemBase.Id, out var equipmentRow))
                    {
                        Debug.LogError($"[ItemRequirementSheet] item id does not exist {itemBase.Id}");
                        return false;
                    }

                    var recipeSheet = sheets.EquipmentItemRecipeSheet;
                    var subRecipeSheet = sheets.EquipmentItemSubRecipeSheetV2;
                    var itemOptionSheet = sheets.EquipmentItemOptionSheet;
                    var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                        recipeSheet,
                        subRecipeSheet,
                        itemOptionSheet
                    );

                    var requirementLevel = isMadeWithMimisbrunnrRecipe ? equipmentRow.MimisLevel : equipmentRow.Level;
                    return currentAvatarState.level >= requirementLevel;

                default:
                    if (!requirementSheet.TryGetValue(itemBase.Id, out var row))
                    {
                        Debug.LogError($"[ItemRequirementSheet] item id does not exist {itemBase.Id}");
                        return false;
                    }

                    return currentAvatarState.level >= row.Level;
            }
        }

        public static int GetItemRequirementLevel(ItemBase itemBase)
        {
            var sheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return 0;
            }

            switch (itemBase.ItemType)
            {
                case ItemType.Equipment:
                    var sheets = Game.Game.instance.TableSheets;
                    var equipment = (Equipment)itemBase;
                    var requirementSheet = sheets.ItemRequirementSheet;
                    if (!requirementSheet.TryGetValue(itemBase.Id, out var equipmentRow))
                    {
                        Debug.LogError($"[ItemRequirementSheet] item id does not exist {itemBase.Id}");
                        return 0;
                    }

                    var recipeSheet = sheets.EquipmentItemRecipeSheet;
                    var subRecipeSheet = sheets.EquipmentItemSubRecipeSheetV2;
                    var itemOptionSheet = sheets.EquipmentItemOptionSheet;
                    var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                        recipeSheet,
                        subRecipeSheet,
                        itemOptionSheet
                    );

                    var requirementLevel = isMadeWithMimisbrunnrRecipe ? equipmentRow.MimisLevel : equipmentRow.Level;
                    return requirementLevel;
                default:
                    return sheet.TryGetValue(itemBase.Id, out var value) ? value.Level : 0;
            }
        }

        public static Player CreatePlayer(AvatarState avatarState, Vector3 position)
        {
            var player = PlayerFactory.Create(avatarState).GetComponent<Player>();
            var t = player.transform;
            t.localScale = Vector3.one;
            t.position = position;
            player.gameObject.SetActive(true);
            return player;
        }

        public static int GetGridItemCount(float cellSize, float spacing, float size)
        {
            var s = size;
            var count = 0;
            while (s > cellSize)
            {
                s -= cellSize;
                s -= spacing;
                count++;
                if (s < 0)
                {
                    return count;
                }
            }

            return count;
        }
        
        public static bool IsInTime(string begin, string end, bool everyYear = true)
        {
            var now = DateTime.UtcNow;
            if (everyYear)
            {
                begin = $"{now.Year}/{begin}";
                end = $"{now.Year}/{end}";
            }
            var bDt = DateTime.ParseExact(begin, "yyyy/MM/dd HH:mm:ss", null);
            var eDt = DateTime.ParseExact(end, "yyyy/MM/dd HH:mm:ss", null);
            var bDiff = (now - bDt).TotalSeconds;
            var eDiff = (eDt - now).TotalSeconds;
            return bDiff > 0 && eDiff > 0;
        }
    }
}
