using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public abstract class ActionHandler
    {
        public bool Pending { get; set; }
        public Currency GoldCurrency { get; internal set; }

        public abstract void Start(ActionRenderer renderer);

        protected static bool HasUpdatedAssetsForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedFungibleAssets.ContainsKey(States.Instance.AgentState.address);
        }

        protected static bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.address);

        protected static bool ValidateEvaluationForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return !(States.Instance.AgentState is null) && evaluation.Signer.Equals(States.Instance.AgentState.address);
        }

        protected static AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetGoldBalanceState(agentAddress, GoldCurrency);
        }

        protected void UpdateCrystalBalance<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var currency = new Currency("CRYSTAL", 18, minter: null);
            var crystal = evaluation.OutputStates.GetBalance(evaluation.Signer, currency);
            ReactiveCrystalState.UpdateCrystal(crystal);
        }

        protected async UniTask UpdateAgentStateAsync<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAgentState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
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

        protected static async UniTask UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            if (evaluation.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
            {
                await UpdateAvatarState(avatarState, index);
            }
        }

        protected async UniTask UpdateCurrentAvatarStateAsync<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (evaluation.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState, out _))
            {
                await UpdateCurrentAvatarStateAsync(avatarState);
            }
        }

        protected static void UpdateWeeklyArenaState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) evaluation.BlockIndex / gameConfigState.WeeklyArenaInterval;
            var weeklyArenaState = evaluation.OutputStates.GetWeeklyArenaState(WeeklyArenaState.DeriveAddress(index));
            States.Instance.SetWeeklyArenaState(weeklyArenaState);
        }

        protected static void UpdateGameConfigState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = evaluation.OutputStates.GetGameConfigState();
            States.Instance.SetGameConfigState(state);
        }

        private static UniTask UpdateAgentStateAsync(AgentState state)
        {
            UpdateCache(state);
            return States.Instance.SetAgentStateAsync(state);
        }

        private static void UpdateGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is { } &&
                Game.Game.instance.Agent.Address.Equals(goldBalanceState.address))
            {
                Game.Game.instance.CachedBalance[goldBalanceState.address] = goldBalanceState.Gold;
            }

            States.Instance.SetGoldBalanceState(goldBalanceState);
        }

        private static UniTask UpdateAvatarState(AvatarState avatarState, int index) =>
            States.Instance.AddOrReplaceAvatarStateAsync(avatarState, index);

        public async UniTask UpdateCurrentAvatarStateAsync(AvatarState avatarState)
        {
            // When in battle, do not immediately update the AvatarState, but pending it.
            if (Pending)
            {
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

        internal static void UpdateCombinationSlotState(int slotIndex, CombinationSlotState state)
        {
            States.Instance.UpdateCombinationSlotState(slotIndex, state);
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
