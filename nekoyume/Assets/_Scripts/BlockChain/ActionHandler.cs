using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public abstract class ActionHandler
    {
        public bool Pending;

        protected bool ValidateEvaluationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase
        {
            if (States.Instance.AgentState.Value == null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.AgentState.Value.address);
        }

        protected bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState.Value is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.Value.address);

        protected AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.Value.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        protected void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAgentState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            var state = GetAgentState(evaluation);
            UpdateAgentState(state);
        }

        protected void UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.Value.avatarAddresses.ContainsKey(index))
            {
                States.Instance.AvatarStates.Remove(index);
                AvatarManager.DeleteAvatarPrivateKey(index);
                return;
            }

            var avatarAddress = States.Instance.AgentState.Value.avatarAddresses[index];
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
            var avatarState = evaluation.OutputStates.GetAvatarState(States.Instance.CurrentAvatarState.Value.address);
            UpdateCurrentAvatarState(avatarState);
        }

        protected void UpdateShopState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.ShopState.Value = new ShopState(
                (Bencodex.Types.Dictionary) evaluation.OutputStates.GetState(ShopState.Address)
            );
        }

        protected void UpdateRankingState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.RankingState.Value = new RankingState(
                (Bencodex.Types.Dictionary) evaluation.OutputStates.GetState(RankingState.Address)
            );
        }

        private void UpdateAvatarState(AvatarState avatarState, int index)
        {
            if (States.Instance.AvatarStates.ContainsKey(index))
            {
                States.Instance.AvatarStates[index] = avatarState;
            }
            else
            {
                States.Instance.AvatarStates.Add(index, avatarState);
            }
        }

        protected static void UpdateAgentState(AgentState state)
        {
            States.Instance.AgentState.Value = state;
        }

        public void UpdateLocalAvatarState(AvatarState avatarState, int index)
        {
            Debug.LogFormat("Update local avatarState. agentAddress: {0} address: {1} BlockIndex: {2}",
                avatarState.agentAddress, avatarState.address, avatarState.blockIndex);
            UpdateAvatarState(avatarState, index);
        }

        public static void UpdateLocalAgentState(AgentState agentState)
        {
            Debug.LogFormat("Update local agentSTate. agentAddress: {0} BlockIndex: {1}",
                agentState.address, Game.Game.instance.Agent.BlockIndex);
            UpdateAgentState(agentState);
        }

        public void UpdateCurrentAvatarState(AvatarState avatarState)
        {
            if (Pending)
            {
                Game.Game.instance.Stage.AvatarState = avatarState;
                return;
            }

            Game.Game.instance.Stage.AvatarState = null;
            var questList = avatarState.questList.Where(i => i.Complete && !i.Receive).ToList();
            if (questList.Count >= 1)
            {
                if (questList.Count == 1)
                {
                    var quest = questList.First();
                    var format = LocalizationManager.Localize("NOTIFICATION_QUEST_COMPLETE");
                    var msg = string.Format(format, quest.GetName());
                    UI.Notification.Push(MailType.System, msg);
                }
                else
                {
                    var format = LocalizationManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.Notification.Push(MailType.System, msg);
                }
            }

            UpdateAvatarState(avatarState, States.Instance.CurrentAvatarKey.Value);
        }
    }
}
