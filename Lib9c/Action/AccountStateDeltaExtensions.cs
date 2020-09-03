using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
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
                Log.Warning("No avatar state ({0})", address.ToHex());
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
                    "Invalid avatar state ({0}): {1}",
                    address.ToHex(),
                    serializedAvatar
                );

                return null;
            }
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
                Log.Error(
                    "The avatar {0} does not belong to the agent {1}.",
                    avatarAddress.ToHex(),
                    agentAddress.ToHex()
                );

                return false;
            }

            avatarState = states.GetAvatarState(avatarAddress);
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
            var value = states.GetState(address);
            if (value is null)
            {
                Log.Warning($"{nameof(T)} is null ({0})", address.ToHex());
                throw new FailedLoadStateException(nameof(T));
            }

            try
            {
                var csv = value.ToDotnetString();
                var sheet = new T();
                sheet.Set(csv);
                return sheet;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(T)}()");
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
    }
}
