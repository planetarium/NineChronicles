using System;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public abstract class ActionHandler
    {
        public bool Pending;
        public Currency GoldCurrency { get; internal set; }

        protected bool ValidateEvaluationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.AgentState.address);
        }

        protected bool HasUpdatedAssetsForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState is null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedFungibleAssets.ContainsKey(States.Instance.AgentState.address);
        }

        protected bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.address);

        protected bool ValidateEvaluationForCurrentAgent<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            return !(States.Instance.AgentState is null) && evaluation.Signer.Equals(States.Instance.AgentState.address);
        }

        protected AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        protected GoldBalanceState GetGoldBalanceState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.address;
            return evaluation.OutputStates.GetGoldBalanceState(agentAddress, GoldCurrency);
        }

        protected void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAgentState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            UpdateAgentState(GetAgentState(evaluation));
            try
            {
                UpdateGoldBalanceState(GetGoldBalanceState(evaluation));
            }
            catch (BalanceDoesNotExistsException)
            {
                UpdateGoldBalanceState(null);
            }
        }

        protected void UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.avatarAddresses.ContainsKey(index))
            {
                States.Instance.RemoveAvatarState(index);
                return;
            }

            var avatarAddress = States.Instance.AgentState.avatarAddresses[index];
            var avatarState = evaluation.OutputStates.GetAvatarState(avatarAddress);
            if (avatarState is null)
            {
                return;
            }

            UpdateAvatarState(avatarState, index);
        }

        protected void UpdateCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            //전투중이면 게임에서의 아바타상태를 바로 업데이트하지말고 쌓아둔다.
            var avatarState = evaluation.OutputStates.GetAvatarState(States.Instance.CurrentAvatarState.address);
            UpdateCurrentAvatarState(avatarState);
        }

        [Obsolete("Do not use ActionBase.ActionEvaluation<T>.OutputStates.GetState() method with ShopState.Address or Addresses.Shop.")]
        protected void UpdateShopState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.SetShopState(new ShopState(
                (Bencodex.Types.Dictionary) evaluation.OutputStates.GetState(ShopState.Address)
            ));
        }

        protected void UpdateWeeklyArenaState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) evaluation.BlockIndex / gameConfigState.WeeklyArenaInterval;
            var weeklyArenaState = evaluation.OutputStates.GetWeeklyArenaState(WeeklyArenaState.DeriveAddress(index));
            States.Instance.SetWeeklyArenaState(weeklyArenaState);
        }

        protected void UpdateGameConfigState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var state = evaluation.OutputStates.GetGameConfigState();
            States.Instance.SetGameConfigState(state);
        }

        protected void UpdateRankingMapState<T>(ActionBase.ActionEvaluation<T> evaluation, Address address) where T : ActionBase
        {
            var value = evaluation.OutputStates.GetState(address);
            if (!(value is null))
            {
                var state = new RankingMapState((Dictionary) value);
                States.Instance.SetRankingMapStates(state);
            }
        }

        private static void UpdateAgentState(AgentState state)
        {
            States.Instance.SetAgentState(state);
        }

        private static void UpdateGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            States.Instance.SetGoldBalanceState(goldBalanceState);
        }

        private void UpdateAvatarState(AvatarState avatarState, int index)
        {
            States.Instance.AddOrReplaceAvatarState(avatarState, index);
        }

        protected static void UpdateCombinationSlotState(CombinationSlotState state)
        {
            States.Instance.SetCombinationSlotState(state);
        }

        public void UpdateCurrentAvatarState(AvatarState avatarState)
        {
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
                    UI.Notification.Push(MailType.System, msg);
                }
                else
                {
                    var format = L10nManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.Notification.Push(MailType.System, msg);
                }
            }

            UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey);
        }
    }
}
