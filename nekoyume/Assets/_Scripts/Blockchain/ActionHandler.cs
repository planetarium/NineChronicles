using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
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
            return !(States.Instance.AgentState is null) &&
                evaluation.Signer.Equals(States.Instance.AgentState.address);
        }

        protected static AgentState GetAgentState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return StateGetter.GetAgentState(agentAddress, evaluation.OutputState);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            if (!evaluation.Signer.Equals(agentAddress))
            {
                return null;
            }

            return StateGetter.GetGoldBalanceState(
                agentAddress,
                GoldCurrency,
                evaluation.OutputState);
        }

        // NOTE: The deposit in returned tuple is get from the IAgent not evaluation.OutputState.
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
            if (!StateGetter.TryGetStakeStateV2(agentAddr, evaluation.OutputState, out var stakeStateV2))
            {
                return (stakeAddr, null, new FungibleAssetValue(), 0, null, null);
            }

            try
            {
                var agent = Game.Game.instance.Agent;
                if (agent is null)
                {
                    return (stakeAddr, null, new FungibleAssetValue(), 0, null, null);
                }

                var balance = await agent.GetBalanceAsync(stakeAddr, GoldCurrency);
                var sheetAddrArr = new[]
                {
                    Addresses.GetSheetAddress(
                        stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName),
                    Addresses.GetSheetAddress(
                        stakeStateV2.Contract.StakeRegularRewardSheetTableName),
                };
                var sheetStates = await agent.GetStateBulkAsync(sheetAddrArr);
                var stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                stakeRegularFixedRewardSheet.Set(
                    sheetStates[sheetAddrArr[0]].ToDotnetString());
                var stakeRegularRewardSheet = new StakeRegularRewardSheet();
                stakeRegularRewardSheet.Set(
                    sheetStates[sheetAddrArr[1]].ToDotnetString());
                var level = stakeRegularFixedRewardSheet.FindLevelByStakedAmount(
                    agentAddr,
                    balance);
                return (
                    stakeAddr,
                    stakeStateV2,
                    balance,
                    level,
                    stakeRegularFixedRewardSheet,
                    stakeRegularRewardSheet);
            }
            catch (Exception)
            {
                return (stakeAddr, null, new FungibleAssetValue(), 0, null, null);
            }
        }

        protected static CrystalRandomSkillState GetCrystalRandomSkillState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var buffStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            if (StateGetter.GetState(buffStateAddress, evaluation.OutputState) is
                Bencodex.Types.List serialized)
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
                Game.Game.instance.Agent.GetAvatarStatesAsync(new[]
                {
                    avatarAddress
                }, evaluation.OutputState).Result[avatarAddress];

            await UpdateAvatarState(state, index);
        }

        protected async UniTask UpdateCurrentAvatarStateAsync<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            try
            {
                await UpdateCurrentAvatarStateAsync(
                    States.Instance.CurrentAvatarState
                        .UpdateAvatarStateV2(avatarAddress, evaluation.OutputState));
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to Update AvatarState: {agentAddress}, {avatarAddress}\n{e.Message}");
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatars =
                await Game.Game.instance.Agent.GetAvatarStatesAsync(new[] { avatarAddress });
            if (avatars.TryGetValue(avatarAddress, out var avatarState))
            {
                await UpdateCurrentAvatarStateAsync(avatarState);
            }
            else
            {
                Debug.LogError($"Failed to get AvatarState: {avatarAddress}");
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
            var inventoryAddr = avatarAddr.Derive(LegacyInventoryKey);
            var inventory = StateGetter.GetInventory(inventoryAddr, eval.OutputState);
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
            var state = GetCrystalRandomSkillState(evaluation);

            if (state is { })
            {
                States.Instance.SetCrystalRandomSkillState(state);
            }
        }

        private static UniTask UpdateAgentStateAsync(AgentState state)
        {
            UpdateCache(state);
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
            if (!evaluation.Signer.Equals(States.Instance.AgentState.address))
            {
                return;
            }

            try
            {
                var crystal = StateGetter.GetBalance(
                    evaluation.Signer,
                    CrystalCalculator.CRYSTAL,
                    evaluation.OutputState);
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

        protected async UniTaskVoid UpdateStakeStateAsync<T>(ActionEvaluation<T> evaluation) where T : ActionBase
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
                Debug.Log($"[{nameof(ActionHandler)}] Pending AvatarState");
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

            UpdateCache(avatarState);
            await UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey);
        }

        internal static void UpdateCombinationSlotState(
            Address avatarAddress,
            int slotIndex,
            CombinationSlotState state)
        {
            States.Instance.UpdateCombinationSlotState(avatarAddress, slotIndex, state);
            UpdateCache(state);
        }

        private static void UpdateCache(Model.State.State state)
        {
            if (state is null)
            {
                return;
            }

            if (Game.Game.instance.CachedStates.ContainsKey(state.address))
            {
                Game.Game.instance.CachedStates[state.address] = state.Serialize();
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
                avatarState.address,
                battleType,
                evaluation.OutputState);
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
                avatarState.address,
                battleType,
                evaluation.OutputState);
            States.Instance.UpdateRuneSlotState(runeSlotState);
        }
    }
}
