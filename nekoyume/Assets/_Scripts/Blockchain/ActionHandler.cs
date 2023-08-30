using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
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

        protected static bool HasUpdatedAssetsForCurrentAgent<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputState.Delta.UpdatedFungibleAssets.Any(tuple =>
                tuple.Item1.Equals(States.Instance.AgentState.address));
        }

        protected static bool HasUpdatedAssetsForCurrentAvatar<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return States.Instance.CurrentAvatarState is not null &&
                   evaluation.OutputState.Delta.UpdatedFungibleAssets.Any(tuple =>
                       tuple.Item1.Equals(States.Instance.CurrentAvatarState.address));
        }

        protected static bool ValidateEvaluationForCurrentAvatarState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (!(States.Instance.CurrentAvatarState is null))
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var addresses = new List<Address>
                {
                    avatarAddress,
                };
                string[] keys =
                {
                    LegacyInventoryKey,
                    LegacyWorldInformationKey,
                    LegacyQuestListKey,
                };
                addresses.AddRange(keys.Select(key => avatarAddress.Derive(key)));
                return addresses.Any(a =>
                    evaluation.OutputState.Delta.UpdatedAddresses.Contains(a));
            }

            return false;
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
            return evaluation.OutputState.GetAgentState(agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            if (!evaluation.Signer.Equals(agentAddress))
            {
                return null;
            }

            var updatedFungibleAssets =
                    evaluation.OutputState.Delta.UpdatedFungibleAssets.Where(tuple =>
                    tuple.Item1.Equals(evaluation.Signer));
            if (!updatedFungibleAssets.Any(tuple => tuple.Item2.Equals(GoldCurrency)))
            {
                return null;
            }

            return evaluation.OutputState.GetGoldBalanceState(agentAddress, GoldCurrency);
        }

        protected (MonsterCollectionState, int, FungibleAssetValue) GetMonsterCollectionState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                agentAddress,
                States.Instance.AgentState.MonsterCollectionRound
            );
            if (!(evaluation.OutputState.GetState(monsterCollectionAddress) is Bencodex.Types.Dictionary mcDict))
            {
                return (null, 0, new FungibleAssetValue());
            }

            try
            {
                var balance =
                    evaluation.OutputState.GetBalance(monsterCollectionAddress, GoldCurrency);
                var level =
                    TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                        agentAddress, balance);
                return (new MonsterCollectionState(mcDict), level, balance);
            }
            catch (Exception)
            {
                return (null, 0, new FungibleAssetValue());
            }
        }

        protected (StakeState, int, FungibleAssetValue) GetStakeState<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var stakeAddress = StakeState.DeriveAddress(agentAddress);
            if (!(evaluation.OutputState.GetState(stakeAddress) is Bencodex.Types.Dictionary serialized))
            {
                return (null, 0, new FungibleAssetValue());
            }

            try
            {
                var state = new StakeState(serialized);
                var balance = evaluation.OutputState.GetBalance(
                    state.address,
                    GoldCurrency);
                var level = TableSheets.Instance.StakeRegularRewardSheet.FindLevelByStakedAmount(
                    agentAddress,
                    balance);
                return (state, level, balance);
            }
            catch (Exception)
            {
                return (null, 0, new FungibleAssetValue());
            }
        }

        protected static CrystalRandomSkillState GetCrystalRandomSkillState<T>(
            ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var buffStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
            if (evaluation.OutputState.GetState(buffStateAddress) is
                Bencodex.Types.List serialized)
            {
                var state = new CrystalRandomSkillState(buffStateAddress, serialized);
                return state;
            }

            return null;
        }

        protected async UniTask UpdateAgentStateAsync<T>(
            ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            Debug.LogFormat(
                "Called UpdateAgentState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputState.Delta.UpdatedAddresses));
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
            Debug.LogFormat(
                "Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`",
                evaluation.Action,
                string.Join(",", evaluation.OutputState.Delta.UpdatedAddresses));
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            if (evaluation.OutputState.TryGetAvatarStateV2(
                    agentAddress,
                    avatarAddress,
                    out var avatarState,
                    out _))
            {
                await UpdateAvatarState(avatarState, index);
            }
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

        protected static void UpdateGameConfigState<T>(ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = evaluation.OutputState.GetGameConfigState();
            States.Instance.SetGameConfigState(state);
        }

        protected static void UpdateStakeState(
            StakeState state,
            GoldBalanceState stakedBalanceState,
            int level)
        {
            if (state is { })
            {
                States.Instance.SetStakeState(state, stakedBalanceState, level);
            }
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

            var updatedFungibleAssets =
                evaluation.OutputState.Delta.UpdatedFungibleAssets.Where(tuple =>
                    tuple.Item1.Equals(evaluation.Signer));
            if (!updatedFungibleAssets.Any(tuple => tuple.Item2.Equals(Currencies.Crystal)))
            {
                return;
            }

            try
            {
                var crystal = evaluation.OutputState.GetBalance(
                    evaluation.Signer,
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
    }
}
