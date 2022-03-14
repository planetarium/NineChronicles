using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using LruCacheNet;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        private const int SheetsCacheSize = 100;
        private static readonly LruCache<string, ISheet> SheetsCache = new LruCache<string, ISheet>(SheetsCacheSize);

        public static IAccountStateDelta MarkBalanceChanged(
            this IAccountStateDelta states,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return states.MintAsset(accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return states;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                states = states.TransferAsset(accounts[i - 1], accounts[i], currency * 1, true);
            }

            return states;
        }

        public static bool TryGetState<T>(this IAccountStateDelta states, Address address, out T result)
            where T : IValue
        {
            IValue raw = states.GetState(address);
            if (raw is T v)
            {
                result = v;
                return true;
            }

            Log.Error(
                "Expected a {0}, but got invalid state ({1}): ({2}) {3}",
                typeof(T).Name,
                address.ToHex(),
                raw?.GetType().Name,
                raw
            );
            result = default;
            return false;
        }

        public static AgentState GetAgentState(this IAccountStateDelta states, Address address)
        {
            var serializedAgent = states.GetState(address);
            if (serializedAgent is null)
            {
                Log.Warning("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Bencodex.Types.Dictionary) serializedAgent);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid agent state ({0}): {1}",
                    address.ToHex(),
                    serializedAgent
                );

                return null;
            }
        }

        public static bool TryGetGoldBalance(
            this IAccountStateDelta states,
            Address address,
            Currency currency,
            out FungibleAssetValue balance)
        {
            try
            {
                balance = states.GetBalance(address, currency);
                return true;
            }
            catch (BalanceDoesNotExistsException)
            {
                balance = default;
                return false;
            }
        }

        public static GoldBalanceState GetGoldBalanceState(
            this IAccountStateDelta states,
            Address address,
            Currency currency
        ) => new GoldBalanceState(address, states.GetBalance(address, currency));

        public static Currency GetGoldCurrency(this IAccountStateDelta states)
        {
            if (states.TryGetState(GoldCurrencyState.Address, out Dictionary asDict))
            {
                return new GoldCurrencyState(asDict).Currency;
            }

            throw new InvalidOperationException(
                "The states doesn't contain gold currency.\n" +
                "Check the genesis block."
            );
        }

        public static AvatarState GetAvatarState(this IAccountStateDelta states, Address address)
        {
            var serializedAvatar = states.GetState(address);
            if (serializedAvatar is null)
            {
                Log.Warning("No avatar state ({AvatarAddress})", address.ToHex());
                return null;
            }

            try
            {
                return new AvatarState((Bencodex.Types.Dictionary) serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                    address.ToHex(),
                    serializedAvatar
                );

                return null;
            }
        }

        public static AvatarState GetAvatarStateV2(this IAccountStateDelta states, Address address)
        {
            var addresses = new List<Address>
            {
                address,
            };
            string[] keys =
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey,
            };
            addresses.AddRange(keys.Select(key => address.Derive(key)));
            var serializedValues = states.GetStates(addresses);
            if (!(serializedValues[0] is Dictionary serializedAvatar))
            {
                Log.Warning("No avatar state ({AvatarAddress})", address.ToHex());
                return null;
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var serializedValue = serializedValues[i + 1];
                if (serializedValue is null)
                {
                    throw new FailedLoadStateException($"failed to load {key}.");
                }

                serializedAvatar = serializedAvatar.SetItem(key, serializedValue);
            }
            try
            {
                return new AvatarState(serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                    address.ToHex(),
                    serializedAvatar
                );

                return null;
            }
        }

        public static bool TryGetAvatarState(
            this IAccountStateDelta states,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            var value = states.GetState(avatarAddress);
            if (value is null)
            {
                return false;
            }

            try
            {
                var serializedAvatar = (Dictionary) value;
                if (serializedAvatar["agentAddress"].ToAddress() != agentAddress)
                {
                    return false;
                }

                avatarState = new AvatarState(serializedAvatar);
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public static bool TryGetAvatarStateV2(
            this IAccountStateDelta states,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState,
            out bool migrationRequired
        )
        {
            avatarState = null;
            migrationRequired = false;
            if (states.GetState(avatarAddress) is Dictionary serializedAvatar)
            {
                try
                {
                    if (serializedAvatar[AgentAddressKey].ToAddress() != agentAddress)
                    {
                        return false;
                    }

                    avatarState = GetAvatarStateV2(states, avatarAddress);
                    return true;
                }
                catch (Exception e)
                {
                    // BackWardCompatible.
                    if (e is KeyNotFoundException || e is FailedLoadStateException)
                    {
                        migrationRequired = true;
                        return states.TryGetAvatarState(agentAddress, avatarAddress, out avatarState);
                    }

                    return false;
                }
            }

            return false;
        }

        public static bool TryGetAgentAvatarStates(
            this IAccountStateDelta states,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            agentState = states.GetAgentState(agentAddress);
            if (agentState is null)
            {
                return false;
            }
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                throw new AgentStateNotContainsAvatarAddressException(
                    $"The avatar {avatarAddress.ToHex()} does not belong to the agent {agentAddress.ToHex()}.");
            }

            avatarState = states.GetAvatarState(avatarAddress);
            return !(avatarState is null);
        }

        public static bool TryGetAgentAvatarStatesV2(
            this IAccountStateDelta states,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            agentState = states.GetAgentState(agentAddress);
            if (agentState is null)
            {
                return false;
            }
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                throw new AgentStateNotContainsAvatarAddressException(
                    $"The avatar {avatarAddress.ToHex()} does not belong to the agent {agentAddress.ToHex()}.");
            }

            try
            {
                avatarState = states.GetAvatarStateV2(avatarAddress);
            }
            catch (FailedLoadStateException)
            {
                // BackWardCompatible.
                avatarState = states.GetAvatarState(avatarAddress);
            }
            return !(avatarState is null);
        }

        public static WeeklyArenaState GetWeeklyArenaState(this IAccountStateDelta states, Address address)
        {
            var iValue = states.GetState(address);
            if (iValue is null)
            {
                Log.Warning("No weekly arena state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new WeeklyArenaState(iValue);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid weekly arena state ({0}): {1}",
                    address.ToHex(),
                    iValue
                );

                return null;
            }
        }

        public static WeeklyArenaState GetWeeklyArenaState(this IAccountStateDelta states, int index)
        {
            var address = WeeklyArenaState.DeriveAddress(index);
            return GetWeeklyArenaState(states, address);
        }

        public static CombinationSlotState GetCombinationSlotState(this IAccountStateDelta states,
            Address avatarAddress, int index)
        {
            var address = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    index
                )
            );
            var value = states.GetState(address);
            if (value is null)
            {
                Log.Warning("No combination slot state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new CombinationSlotState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static GameConfigState GetGameConfigState(this IAccountStateDelta states)
        {
            var value = states.GetState(GameConfigState.Address);
            if (value is null)
            {
                Log.Warning("No game config state ({0})", GameConfigState.Address.ToHex());
                return null;
            }

            try
            {
                return new GameConfigState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static RedeemCodeState GetRedeemCodeState(this IAccountStateDelta states)
        {
            var value = states.GetState(RedeemCodeState.Address);
            if (value is null)
            {
                Log.Warning("RedeemCodeState is null. ({0})", RedeemCodeState.Address.ToHex());
                return null;
            }

            try
            {
                return new RedeemCodeState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static IEnumerable<GoldDistribution> GetGoldDistribution(
            this IAccountStateDelta states)
        {
            var value = states.GetState(Addresses.GoldDistribution);
            if (value is null)
            {
                Log.Warning($"{nameof(GoldDistribution)} is null ({0})", Addresses.GoldDistribution.ToHex());
                return null;
            }

            try
            {
                var goldDistributions = (Bencodex.Types.List)value;
                return goldDistributions.Select(v => new GoldDistribution(v));
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetGoldDistribution)}()");
                throw;
            }
        }

        public static T GetSheet<T>(this IAccountStateDelta states) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();

            try
            {
                var csv = GetSheetCsv<T>(states);
                byte[] hash;
                using (var sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(csv));
                }

                var cacheKey = address.ToHex() + ByteUtil.Hex(hash);

                if (SheetsCache.TryGetValue(cacheKey, out ISheet cached))
                {
                    return (T)cached;
                }

                var sheet = new T();
                sheet.Set(csv);
                SheetsCache.AddOrUpdate(cacheKey, sheet);
                return sheet;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during GetSheet<{TypeName}>()", typeof(T).FullName);
                throw;
            }
        }

        public static string GetSheetCsv<T>(this IAccountStateDelta states) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();
            var value = states.GetState(address);
            if (value is null)
            {
                Log.Warning("{TypeName} is null ({Address})", typeof(T).FullName, address.ToHex());
                throw new FailedLoadStateException(typeof(T).FullName);
            }

            try
            {
                return value.ToDotnetString();
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during GetSheetCsv<{TypeName}>()", typeof(T).FullName);
                throw;
            }
        }

        public static ItemSheet GetItemSheet(this IAccountStateDelta states)
        {
            var sheet = new ItemSheet();
            sheet.Set(GetSheet<ConsumableItemSheet>(states), false);
            sheet.Set(GetSheet<CostumeItemSheet>(states), false);
            sheet.Set(GetSheet<EquipmentItemSheet>(states), false);
            sheet.Set(GetSheet<MaterialItemSheet>(states));
            return sheet;
        }

        public static StageSimulatorSheets GetStageSimulatorSheets(this IAccountStateDelta states)
        {
            return new StageSimulatorSheets(
                GetSheet<MaterialItemSheet>(states),
                GetSheet<SkillSheet>(states),
                GetSheet<SkillBuffSheet>(states),
                GetSheet<BuffSheet>(states),
                GetSheet<CharacterSheet>(states),
                GetSheet<CharacterLevelSheet>(states),
                GetSheet<EquipmentItemSetEffectSheet>(states),
                GetSheet<StageSheet>(states),
                GetSheet<StageWaveSheet>(states),
                GetSheet<EnemySkillSheet>(states)
            );
        }

        public static RankingSimulatorSheets GetRankingSimulatorSheets(this IAccountStateDelta states)
        {
            return new RankingSimulatorSheets(
                GetSheet<MaterialItemSheet>(states),
                GetSheet<SkillSheet>(states),
                GetSheet<SkillBuffSheet>(states),
                GetSheet<BuffSheet>(states),
                GetSheet<CharacterSheet>(states),
                GetSheet<CharacterLevelSheet>(states),
                GetSheet<EquipmentItemSetEffectSheet>(states),
                GetSheet<WeeklyArenaRewardSheet>(states)
            );
        }

        public static QuestSheet GetQuestSheet(this IAccountStateDelta states)
        {
            var questSheet = new QuestSheet();
            questSheet.Set(GetSheet<WorldQuestSheet>(states), false);
            questSheet.Set(GetSheet<CollectQuestSheet>(states), false);
            questSheet.Set(GetSheet<CombinationQuestSheet>(states), false);
            questSheet.Set(GetSheet<TradeQuestSheet>(states), false);
            questSheet.Set(GetSheet<MonsterQuestSheet>(states), false);
            questSheet.Set(GetSheet<ItemEnhancementQuestSheet>(states), false);
            questSheet.Set(GetSheet<GeneralQuestSheet>(states), false);
            questSheet.Set(GetSheet<ItemGradeQuestSheet>(states), false);
            questSheet.Set(GetSheet<ItemTypeCollectQuestSheet>(states), false);
            questSheet.Set(GetSheet<GoldQuestSheet>(states), false);
            questSheet.Set(GetSheet<CombinationEquipmentQuestSheet>(states));
            return questSheet;
        }
        public static AvatarSheets GetAvatarSheets(this IAccountStateDelta states)
        {
            return new AvatarSheets(
                GetSheet<WorldSheet>(states),
                GetQuestSheet(states),
                GetSheet<QuestRewardSheet>(states),
                GetSheet<QuestItemRewardSheet>(states),
                GetSheet<EquipmentItemRecipeSheet>(states),
                GetSheet<EquipmentItemSubRecipeSheet>(states)
            );
        }

        public static RankingState GetRankingState(this IAccountStateDelta states)
        {
            var value = states.GetState(Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }
            return new RankingState((Dictionary) value);
        }

        public static RankingState1 GetRankingState1(this IAccountStateDelta states)
        {
            var value = states.GetState(Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState1));
            }
            return new RankingState1((Dictionary) value);
        }

        public static RankingState0 GetRankingState0(this IAccountStateDelta states)
        {
            var value = states.GetState(Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }
            return new RankingState0((Dictionary) value);
        }

        public static ShopState GetShopState(this IAccountStateDelta states)
        {
            var value = states.GetState(Addresses.Shop);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(ShopState));
            }
            return new ShopState((Dictionary) value);
        }
    }
}
