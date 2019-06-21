using System;
using System.Collections.Generic;
using Libplanet.Action;
using Nekoyume.Action;
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

        private void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.agentState.Value = (AgentState) evaluation.OutputStates.GetState(States.Instance.agentState.Value.address);
        }
        
        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == States.Instance.agentState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(eval => eval.InputContext.Signer == States.Instance.agentState.Value.address
                               && eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var index = eval.Action.index;
                    var avatarAddress = AvatarManager.GetOrCreateAvatarAddress(index);
                    States.Instance.agentState.Value.avatarAddresses.Add(index, avatarAddress);
                    States.Instance.avatarStates.Add(index,
                        (AvatarState) AgentController.Agent.GetState(avatarAddress));
                }).AddTo(_disposables);
        }
        
        private void DeleteAvatar()
        {
            ActionBase.EveryRender<DeleteAvatar>()
                .Where(eval => eval.InputContext.Signer == States.Instance.agentState.Value.address
                               && eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var index = eval.Action.index;
                    States.Instance.agentState.Value.avatarAddresses.Remove(index);
                    States.Instance.avatarStates.Remove(index);
                    AvatarManager.DeleteAvatarPrivateKey(index);
                }).AddTo(_disposables);
        }
        
        private void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.Succeed)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var state = (AvatarState) AgentController.Agent.GetState(States.Instance.currentAvatarState.Value.address);
                    foreach (var item in States.Instance.avatarStates)
                    {
                        if (item.Value.address != state.address)
                        {
                            continue;
                        }
                        
                        States.Instance.avatarStates[item.Key] = state;
                        break;
                    }
                    
                    ReactiveCurrentAvatarState.AvatarState.Value = States.Instance.currentAvatarState.Value = state;
                }).AddTo(_disposables);
        }

        private void Combination()
        {
            ActionBase.EveryRender<Combination>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && (eval.Action.Succeed || eval.Action.errorCode == GameAction.ErrorCode.CombinationNoResultItem))
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    foreach (var material in eval.Action.Materials)
                    {
                        States.Instance.currentAvatarState.Value.inventory.RemoveFungibleItem(material.id, material.count);
                    }

                    if (eval.Action.errorCode == GameAction.ErrorCode.CombinationNoResultItem)
                    {
                        return;
                    }
                    
                    foreach (var itemUsable in eval.Action.Results)
                    {
                        States.Instance.currentAvatarState.Value.inventory.AddUnfungibleItem(itemUsable);
                    }
                }).AddTo(_disposables);
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
                        States.Instance.currentAvatarState.Value.inventory.RemoveUnfungibleItem(result.shopItem.itemUsable);
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
                        States.Instance.currentAvatarState.Value.inventory.AddUnfungibleItem(result.shopItem.itemUsable);
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
                        States.Instance.currentAvatarState.Value.inventory.AddUnfungibleItem(result.shopItem.itemUsable);
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
