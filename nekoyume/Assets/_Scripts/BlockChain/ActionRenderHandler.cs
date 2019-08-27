using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Manager;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    ///
    /// ToDo. `ActionRenderHandler`의 형태가 완성되면, `ActionUnrenderHandler`도 작성해야 함.
    /// </summary>
    public class ActionRenderHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        public static readonly ActionRenderHandler Instance = Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private ActionRenderHandler()
        {
        }

        public void Start()
        {
            Shop();
            Ranking();
            RewardGold();
            CreateAvatar();
            DeleteAvatar();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            RankingReward();
            AddItem();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }
        
        private bool ValidateEvaluationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (States.Instance.agentState.Value == null)
            {
                return false;
            }
            return evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.agentState.Value.address);
        }

        private bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.currentAvatarState.Value is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.currentAvatarState.Value.address);

        private AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.agentState.Value.address;
            return (AgentState) evaluation.OutputStates.GetState(agentAddress);
        }

        private void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAgentState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            States.Instance.agentState.Value = GetAgentState(evaluation);
        }

        private void UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.agentState.Value.avatarAddresses.ContainsKey(index))
            {
                States.Instance.avatarStates.Remove(index);
                AvatarManager.DeleteAvatarPrivateKey(index);
                return;
            }

            var avatarAddress = States.Instance.agentState.Value.avatarAddresses[index];
            var avatarState = (AvatarState) evaluation.OutputStates.GetState(avatarAddress);
            if (avatarState == null)
            {
                return;
            }
            
            if (States.Instance.avatarStates.ContainsKey(index))
            {
                States.Instance.avatarStates[index] = avatarState;
            }
            else
            {
                States.Instance.avatarStates.Add(index, avatarState);
            }
        }
        
        private void UpdateCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            UpdateAvatarState(evaluation, States.Instance.currentAvatarKey.Value);
        }
        
        private void UpdateShopState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.shopState.Value = (ShopState) evaluation.OutputStates.GetState(ShopState.Address);
        }
        
        private void UpdateRankingState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.rankingState.Value = (RankingState) evaluation.OutputStates.GetState(RankingState.Address);
        }

        private void Shop()
        {
            ActionBase.EveryRender(ShopState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateShopState).AddTo(_disposables);
        }
        
        private void Ranking()
        {
            ActionBase.EveryRender(RankingState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateRankingState).AddTo(_disposables);
        }

        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void DeleteAvatar()
        {
            ActionBase.EveryRender<DeleteAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void Combination()
        {
            ActionBase.EveryRender<Action.Combination>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombination).AddTo(_disposables);
        }

        private void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateCurrentAvatarState(eval);
                }).AddTo(_disposables);
        }

        private void RankingReward()
        {
            ActionBase.EveryRender<RankingReward>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void AddItem()
        {
            ActionBase.EveryRender<AddItem>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void ResponseCombination(ActionBase.ActionEvaluation<Combination> evaluation)
        {
            var isSuccess = !(evaluation.Action.result.itemUsable is null);
            AnalyticsManager.Instance.OnEvent(isSuccess
                ? AnalyticsManager.EventName.ActionCombinationSuccess
                : AnalyticsManager.EventName.ActionCombinationFail);
            UpdateCurrentAvatarState(evaluation);
            Game.Event.OnCombinationEnd.Invoke(isSuccess);
        }
    }
}
