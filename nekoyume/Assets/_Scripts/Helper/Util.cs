using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using UnityEngine;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.Helper
{
    public static class Util
    {
        public const int VisibleEnhancementEffectLevel = 10;
        private const string StoredSlotIndex = "AutoSelectedSlotIndex_";

        public static string GetBlockToTime(long block)
        {
            if (block < 0)
            {
                return string.Empty;
            }

            const int secondsPerBlock = 12;
            var remainSecond = block * secondsPerBlock;
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
                sb.Append("0m");
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
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null || itemBase is null)
            {
                return false;
            }

            return currentAvatarState.level >= GetItemRequirementLevel(itemBase);
        }

        public static int GetItemRequirementLevel(ItemBase itemBase)
        {
            var sheets = Game.Game.instance.TableSheets;
            var requirementSheet = sheets.ItemRequirementSheet;
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return 0;
            }

            switch (itemBase.ItemType)
            {
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
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

                    return isMadeWithMimisbrunnrRecipe ? equipmentRow.MimisLevel : equipmentRow.Level;
                default:
                    return requirementSheet.TryGetValue(itemBase.Id, out var row) ? row.Level : 0;
            }
        }

        public static bool CanBattle(
            List<Equipment> equipments,
            List<Costume> costumes,
            IEnumerable<int> foodIds)
        {
            var isValidated = false;
            var tableSheets = Game.Game.instance.TableSheets;
            try
            {
                var costumeIds = costumes.Select(costume => costume.Id);
                States.Instance.CurrentAvatarState.ValidateItemRequirement(
                    costumeIds.Concat(foodIds).ToList(),
                    equipments,
                    tableSheets.ItemRequirementSheet,
                    tableSheets.EquipmentItemRecipeSheet,
                    tableSheets.EquipmentItemSubRecipeSheetV2,
                    tableSheets.EquipmentItemOptionSheet,
                    States.Instance.CurrentAvatarState.address.ToHex());
                isValidated = true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Check the player is equipped with the valid equipment.\nException: {e}");
            }

            return isValidated;
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

        public static int TotalCP(BattleType battleType)
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var (equipments, costumes) = States.Instance.GetEquippedItems(battleType);
            var runeStated = States.Instance.GetEquippedRuneStates(battleType);
            var runeOptionInfos = GetRuneOptions(runeStated, runeOptionSheet);
            return CPHelper.TotalCP(equipments, costumes, runeOptionInfos, level, row, costumeSheet);
        }

        public static List<RuneOptionSheet.Row.RuneOptionInfo> GetRuneOptions(
            List<RuneState> runeStates,
            RuneOptionSheet sheet)
        {
            var result = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in runeStates)
            {
                if (!sheet.TryGetValue(runeState.RuneId, out var row))
                {
                    continue;
                }

                if (!row.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                result.Add(statInfo);
            }

            return result;
        }

        public static int GetRuneCp(RuneState runeState)
        {
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
            {
                return 0;
            }
            if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
            {
                return 0;
            }

            return option.Cp;
        }

        public static int GetPortraitId(List<Equipment> equipments, List<Costume> costumes)
        {
            var id = GameConfig.DefaultAvatarArmorId;
            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            if (armor != null)
            {
                id = armor.Id;
            }

            var fullCostume = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume != null)
            {
                id = fullCostume.Id;
            }

            return id;
        }

        public static int GetPortraitId(BattleType battleType)
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(battleType);
            return GetPortraitId(equipments, costumes);
        }

        public static int GetArmorId()
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var id = GameConfig.DefaultAvatarArmorId;
            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            if (armor != null)
            {
                id = armor.Id;
            }

            return id;
        }
    }
}
