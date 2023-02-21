using System;
using System.Globalization;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class Addresses
    {
        public static readonly Address Shop                  = new Address("0000000000000000000000000000000000000000");
        public static readonly Address Ranking               = new Address("0000000000000000000000000000000000000001");
        public static readonly Address WeeklyArena           = new Address("0000000000000000000000000000000000000002");
        public static readonly Address TableSheet            = new Address("0000000000000000000000000000000000000003");
        public static readonly Address GameConfig            = new Address("0000000000000000000000000000000000000004");
        public static readonly Address RedeemCode            = new Address("0000000000000000000000000000000000000005");
        public static readonly Address Admin                 = new Address("0000000000000000000000000000000000000006");
        public static readonly Address PendingActivation     = new Address("0000000000000000000000000000000000000007");
        public static readonly Address ActivatedAccount      = new Address("0000000000000000000000000000000000000008");
        public static readonly Address Blacksmith            = new Address("0000000000000000000000000000000000000009");
        public static readonly Address GoldCurrency          = new Address("000000000000000000000000000000000000000a");
        public static readonly Address GoldDistribution      = new Address("000000000000000000000000000000000000000b");
        public static readonly Address AuthorizedMiners      = new Address("000000000000000000000000000000000000000c");
        public static readonly Address Credits               = new Address("000000000000000000000000000000000000000d");
        public static readonly Address UnlockWorld           = new Address("000000000000000000000000000000000000000e");
        public static readonly Address UnlockEquipmentRecipe = new Address("000000000000000000000000000000000000000f");
        public static readonly Address MaterialCost          = new Address("0000000000000000000000000000000000000010");
        public static readonly Address StageRandomBuff       = new Address("0000000000000000000000000000000000000011");
        public static readonly Address Arena                 = new Address("0000000000000000000000000000000000000012");
        public static readonly Address SuperCraft            = new Address("0000000000000000000000000000000000000013");
        public static readonly Address EventDungeon          = new Address("0000000000000000000000000000000000000014");
        public static readonly Address Raid                  = new Address("0000000000000000000000000000000000000015");
        public static readonly Address Rune                  = new Address("0000000000000000000000000000000000000016");

        public static Address GetSheetAddress<T>() where T : ISheet => GetSheetAddress(typeof(T).Name);

        public static Address GetSheetAddress(string sheetName) => TableSheet.Derive(sheetName);

        public static Address GetItemAddress(Guid itemId) => Blacksmith.Derive(itemId.ToString());

        public static Address GetDailyCrystalCostAddress(int index)
        {
            return MaterialCost.Derive($"daily_{index.ToString(CultureInfo.InvariantCulture)}");
        }

        public static Address GetWeeklyCrystalCostAddress(int index)
        {
            return MaterialCost.Derive($"weekly_{index.ToString(CultureInfo.InvariantCulture)}");
        }

        public static Address GetSkillStateAddressFromAvatarAddress(Address avatarAddress) =>
            avatarAddress.Derive("has_buff");

        public static Address GetShopFeeAddress(int championshipId, int round) => Shop.Derive($"_{championshipId}_{round}");

        public static Address GetBlacksmithFeeAddress(int championshipId, int round) => Blacksmith.Derive($"_{championshipId}_{round}");

        public static Address GetRuneFeeAddress(int championshipId, int round) => Rune.Derive($"_{championshipId}_{round}");

        public static Address GetHammerPointStateAddress(Address avatarAddress, int recipeId) =>
            avatarAddress.Derive($"hammer_{recipeId}");

        public static Address GetWorldBossAddress(int raidId) => Raid.Derive($"{raidId}");
        public static Address GetWorldBossKillRewardRecordAddress(Address avatarAddress, int raidId) => avatarAddress.Derive($"reward_info_{raidId}");
        public static Address GetRaiderAddress(Address avatarAddress, int raidId) => avatarAddress.Derive($"{raidId}");

        public static Address GetRaiderListAddress(int raidId) =>
            Raid.Derive($"raider_list_{raidId}");

        public static Address GetAvatarAddress(Address agentAddr, int index)
        {
            if (index < 0 ||
                index >= Nekoyume.GameConfig.SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index must be between 0 and {Nekoyume.GameConfig.SlotCount - 1}.");
            }

            return agentAddr.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    index
                ));
        }

        public static bool IsContainedInAgent(Address agentAddr, Address avatarAddr) =>
            Enumerable.Range(0, Nekoyume.GameConfig.SlotCount)
                .Select(index => GetAvatarAddress(agentAddr, index))
                .Contains(avatarAddr);
    }
}
