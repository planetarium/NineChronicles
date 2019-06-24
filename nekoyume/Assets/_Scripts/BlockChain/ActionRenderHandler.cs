using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx;

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
            RewardGold();
            CreateAvatar();
            DeleteAvatar();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            Ranking();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }
        
        private bool EvaluationValidationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (States.Instance.agentState.Value == null)
            {
                return false;
            }
            
            return evaluation.InputContext.Signer == States.Instance.agentState.Value.address;
        }
        
        private bool EvaluationValidationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (States.Instance.currentAvatarState.Value == null)
            {
                return false;
            }
            
            return evaluation.InputContext.Signer == States.Instance.currentAvatarState.Value.address;
        }

        private AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.agentState.Value.address;
            return (AgentState) evaluation.OutputStates.GetState(agentAddress);
        }
        
        private AvatarState GetAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            if (!States.Instance.agentState.Value.avatarAddresses.ContainsKey(index))
            {
                return null;
            }
            
            var avatarAddress = States.Instance.agentState.Value.avatarAddresses[index];
            return (AvatarState) evaluation.OutputStates.GetState(avatarAddress);
        }
        
        private AvatarState GetCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            return GetAvatarState(evaluation, States.Instance.currentAvatarKey.Value);
        }

        private void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.agentState.Value = GetAgentState(evaluation);
        }

        private void UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
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

        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(EvaluationValidationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(EvaluationValidationForAgentState)
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
                .Where(EvaluationValidationForAgentState)
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
                .Where(EvaluationValidationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void Combination()
        {
            ActionBase.EveryRender<Combination>()
                .Where(EvaluationValidationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    if (eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address)
                    {
                        States.Instance.currentAvatarState.Value.inventory.RemoveUnfungibleItem(result.shopItem
                            .itemUsable);
                    }

                    ShopState.Register(ReactiveShopState.Items, States.Instance.currentAvatarState.Value.address,
                        result.shopItem);
                }).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    if (eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address)
                    {
                        States.Instance.currentAvatarState.Value.inventory
                            .AddUnfungibleItem(result.shopItem.itemUsable);
                    }

                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(_disposables);
        }

        private void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(eval => eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    if (eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address)
                    {
                        States.Instance.currentAvatarState.Value.inventory
                            .AddUnfungibleItem(result.shopItem.itemUsable);
                    }

                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(_disposables);
        }

        private void Ranking()
        {
            ActionBase.EveryRender(RankingState.Address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var asGameAction = eval.Action as GameAction;
                    if (asGameAction is null || asGameAction.Succeed)
                    {
                        var state = (RankingState) eval.OutputStates.GetState(RankingState.Address);
                        ReactiveRankingState.RankingState.Value = States.Instance.rankingState.Value = state;
                    }
                })
                .AddTo(_disposables);
        }
    }
}
