using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Blockchain
{
    public abstract class ActionHandler
    {
        public bool Pending { get; set; }
        public Currency GoldCurrency { get; internal set; }

        public abstract void Start(ActionRenderer renderer);

        protected static bool ValidateEvaluationIsSuccess<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return evaluation.Exception is null;
        }

        protected static bool ValidateEvaluationIsTerminated<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return evaluation.Exception is not null;
        }

        protected static bool ValidateEvaluationForCurrentAgent<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return States.Instance?.AgentState is not null &&
                evaluation.Signer.Equals(States.Instance.AgentState.address);
        }

        protected static AgentState GetAgentState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return StateGetter.GetAgentState(evaluation.OutputState, agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;

            return StateGetter.GetGoldBalanceState(
                evaluation.OutputState,
                agentAddress,
                GoldCurrency);
        }

        protected async UniTask<(
                Address stakeAddr,
                StakeStateV2? stakeStateV2,
                FungibleAssetValue deposit,
                int stakingLevel,
                StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet,
                StakeRegularRewardSheet stakeRegularRewardSheet)>
            GetStakeStateAsync<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddr = States.Instance.AgentState.address;
            var stakeAddr = StakeStateV2.DeriveAddress(agentAddr);
            var agent = Game.Game.instance.Agent;
            Address[] sheetAddrArr;
            FungibleAssetValue balance;
            StakeStateV2? nullableStakeState;
            if (!StateGetter.TryGetStakeStateV2(evaluation.OutputState, agentAddr, out var stakeStateV2))
            {
                nullableStakeState = null;
                var policySheet = TableSheets.Instance.StakePolicySheet;
                sheetAddrArr = new[]
                {
                    Addresses.GetSheetAddress(policySheet.StakeRegularFixedRewardSheetValue),
                    Addresses.GetSheetAddress(policySheet.StakeRegularRewardSheetValue)
                };
                balance = GoldCurrency * 0;
            }
            else
            {
                nullableStakeState = stakeStateV2;
                balance = StateGetter.GetBalance(evaluation.OutputState, stakeAddr, GoldCurrency);
                sheetAddrArr = new[]
                {
                    Addresses.GetSheetAddress(
                        stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName),
                    Addresses.GetSheetAddress(
                        stakeStateV2.Contract.StakeRegularRewardSheetTableName),
                };
            }

            try
            {
                if (agent is null)
                {
                    return (stakeAddr, null, new FungibleAssetValue(), 0, null, null);
                }

                var sheetStates = await agent.GetStateBulkAsync(
                    ReservedAddresses.LegacyAccount, sheetAddrArr);
                var stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                stakeRegularFixedRewardSheet.Set(
                    sheetStates[sheetAddrArr[0]].ToDotnetString());
                var stakeRegularRewardSheet = new StakeRegularRewardSheet();
                stakeRegularRewardSheet.Set(
                    sheetStates[sheetAddrArr[1]].ToDotnetString());
                var level = nullableStakeState.HasValue
                    ? stakeRegularFixedRewardSheet.FindLevelByStakedAmount(
                        agentAddr,
                        balance)
                    : 0;

                return (
                    stakeAddr,
                    nullableStakeState,
                    balance,
                    level,
                    stakeRegularFixedRewardSheet,
                    stakeRegularRewardSheet);
            }
            catch (Exception e)
            {
                return (stakeAddr, null, new FungibleAssetValue(), 0, null, null);
            }
        }

        protected static CrystalRandomSkillState GetCrystalRandomSkillState(
            HashDigest<SHA256> states)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var buffStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            if (StateGetter.GetState(
                    states,
                    ReservedAddresses.LegacyAccount,
                    buffStateAddress) is Bencodex.Types.List serialized)
            {
                var state = new CrystalRandomSkillState(buffStateAddress, serialized);
                return state;
            }

            return null;
        }

        public static void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            if (avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var questList = States.Instance.CurrentAvatarState.questList;
            foreach (var id in ids)
            {
                var quest = questList.FirstOrDefault(q => q.Id == id);
                if (quest == null)
                {
                    continue;
                }

                var rewardMap = quest.Reward.ItemMap;

                foreach (var reward in rewardMap)
                {
                    var materialRow = TableSheets.Instance
                        .MaterialItemSheet
                        .First(pair => pair.Key == reward.Item1);

                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        materialRow.Value.ItemId,
                        reward.Item2);
                }

                LocalLayerModifier.AddReceivableQuest(avatarAddress, id);
            }
        }

        protected async UniTask UpdateAgentStateAsync<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            await UpdateAgentStateAsync(GetAgentState(evaluation));
            try
            {
                UpdateGoldBalanceState(GetGoldBalanceState(evaluation));
            }
            catch (BalanceDoesNotExistsException)
            {
                UpdateGoldBalanceState(null);
            }

            UpdateCrystalBalance(evaluation);
        }

        protected static async UniTask UpdateAvatarState<T>(
            ActionEvaluation<T> evaluation,
            int index)
            where T : ActionBase
        {
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            var state =
                Game.Game.instance.Agent.GetAvatarStatesAsync(
                    evaluation.OutputState,
                    new[] { avatarAddress }).Result[avatarAddress];

            await UpdateAvatarState(state, index);
        }

        protected async UniTask UpdateCurrentAvatarStateAsync<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            try
            {
                await UpdateCurrentAvatarStateAsync(
                    Game.Game.instance.Agent.GetAvatarStatesAsync(
                        evaluation.OutputState,
                        new [] { avatarAddress }).Result[avatarAddress]);
            }
            catch (Exception e)
            {
                NcDebug.LogError(
                    $"Failed to Update AvatarState: {agentAddress}, {avatarAddress}\n{e.Message}");
            }
        }

        protected static void UpdateCurrentAvatarInventory<T>(ActionEvaluation<T> eval)
            where T : ActionBase
        {
            var states = States.Instance;
            if (states.CurrentAvatarState is null)
            {
                return;
            }

            var avatarAddr = states.CurrentAvatarState.address;
            var inventory = StateGetter.GetInventory(eval.OutputState, avatarAddr);
            var avatarState = states.CurrentAvatarState;
            avatarState.inventory = inventory;
            avatarState = LocalLayer.Instance.ModifyInventoryOnly(avatarState);
            ReactiveAvatarState.UpdateInventory(avatarState.inventory);
        }

        protected static void UpdateGameConfigState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (evaluation.Action is not PatchTableSheet
            {
                TableName: nameof(GameConfigSheet),
            })
            {
                return;
            }

            var state = StateGetter.GetGameConfigState(evaluation.OutputState);
            States.Instance.SetGameConfigState(state);
        }

        protected static void UpdateCrystalRandomSkillState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = GetCrystalRandomSkillState(evaluation.OutputState);

            if (state is { })
            {
                States.Instance.SetCrystalRandomSkillState(state);
            }
        }

        private static UniTask UpdateAgentStateAsync(AgentState state)
        {
            UpdateCache(Addresses.Agent, state);
            return States.Instance.SetAgentStateAsync(state);
        }

        private static void UpdateGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            var game = Game.Game.instance;
            if (goldBalanceState is { } &&
                game.Agent.Address.Equals(goldBalanceState.address))
            {
                var currency = goldBalanceState.Gold.Currency;
                if (!game.CachedBalance.ContainsKey(currency))
                {
                    game.CachedBalance[currency] =
                        new LruCache<Address, FungibleAssetValue>(2);
                }

                game.CachedBalance[currency][goldBalanceState.address] =
                    goldBalanceState.Gold;
            }

            States.Instance.SetGoldBalanceState(goldBalanceState);
        }

        protected static void UpdateCrystalBalance<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            try
            {
                var crystal = StateGetter.GetBalance(
                    evaluation.OutputState,
                    States.Instance.AgentState.address,
                    CrystalCalculator.CRYSTAL);
                States.Instance.SetCrystalBalance(crystal);
            }
            catch (BalanceDoesNotExistsException)
            {
                var crystal = FungibleAssetValue.FromRawValue(
                    CrystalCalculator.CRYSTAL,
                    0);
                States.Instance.SetCrystalBalance(crystal);
            }
        }

        protected async UniTask UpdateStakeStateAsync<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var (
                    stakeAddr,
                    stakeStateV2,
                    deposit,
                    stakingLevel,
                    stakeRegularFixedRewardSheet,
                    stakeRegularRewardSheet) =
                await GetStakeStateAsync(evaluation);
            States.Instance.SetStakeState(
                stakeStateV2,
                new GoldBalanceState(stakeAddr, deposit),
                stakingLevel,
                stakeRegularFixedRewardSheet,
                stakeRegularRewardSheet);
        }

        private static UniTask UpdateAvatarState(AvatarState avatarState, int index) =>
            States.Instance.AddOrReplaceAvatarStateAsync(avatarState, index);

        public async UniTask UpdateCurrentAvatarStateAsync(AvatarState avatarState)
        {
            // When in battle, do not immediately update the AvatarState, but pending it.
            if (Pending)
            {
                NcDebug.Log($"[{nameof(ActionHandler)}] Pending AvatarState");
                Game.Game.instance.Stage.AvatarState = avatarState;
                return;
            }

            Game.Game.instance.Stage.AvatarState = null;
            var questList = avatarState.questList.Where(i => i.Complete && !i.IsPaidInAction).ToList();
            if (questList.Count >= 1)
            {
                if (questList.Count == 1)
                {
                    var quest = questList.First();
                    var format = L10nManager.Localize("NOTIFICATION_QUEST_COMPLETE");
                    var msg = string.Format(format, quest.GetContent());
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
                else
                {
                    var format = L10nManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.NotificationSystem.Push(MailType.System, msg, NotificationCell.NotificationType.Information);
                }
            }

            UpdateCache(Addresses.Avatar, avatarState);
            await UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey);
        }

        protected static long GetActionPoint<T>(ActionEvaluation<T> evaluation, Address avatarAddress) where T : ActionBase
        {
            return (Bencodex.Types.Integer)StateGetter.GetState(evaluation.OutputState,
                Addresses.ActionPoint,
                avatarAddress);
        }

        internal static void UpdateCombinationSlotState(
            Address avatarAddress,
            int slotIndex,
            CombinationSlotState state)
        {
            States.Instance.UpdateCombinationSlotState(avatarAddress, slotIndex, state);
            UpdateCache(ReservedAddresses.LegacyAccount, state);
        }

        private static void UpdateCache(Address accountAddress, Model.State.State state)
        {
            if (state is null)
            {
                return;
            }

            if (Game.Game.instance.CachedStates.ContainsKey(accountAddress.Derive(state.address.ToByteArray())))
            {
                try
                {
                    Game.Game.instance.CachedStates[accountAddress.Derive(state.address.ToByteArray())] = state.Serialize();
                }
                catch (NotSupportedException)
                {
                    Game.Game.instance.CachedStates[accountAddress.Derive(state.address.ToByteArray())] = state.SerializeList();
                }
            }
        }

        protected static void UpdateCurrentAvatarItemSlotState<T>(
            ActionEvaluation<T> evaluation,
            BattleType battleType) where T : ActionBase
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            var itemSlotState = StateGetter.GetItemSlotState(
                evaluation.OutputState,
                avatarState.address,
                battleType);
            States.Instance.UpdateItemSlotState(itemSlotState);
        }

        protected static void UpdateCurrentAvatarRuneSlotState<T>(
            ActionEvaluation<T> evaluation,
            BattleType battleType) where T : ActionBase
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            var runeSlotState = StateGetter.GetRuneSlotState(
                evaluation.OutputState,
                avatarState.address,
                battleType);
            States.Instance.UpdateRuneSlotState(runeSlotState);
        }

        protected static void UpdateCurrentAvatarRuneStoneBalance<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var runeSheet = TableSheets.Instance.RuneSheet;
            foreach (var row in runeSheet.Values)
            {
                States.Instance.SetCurrentAvatarBalance(
                    StateGetter.GetBalance(
                        evaluation.OutputState,
                        avatarAddress,
                        RuneHelper.ToCurrency(row)));
            }
        }
    }
}
