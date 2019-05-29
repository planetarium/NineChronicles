using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    public static class ActionRenderHandler
    {
        private static readonly List<IDisposable> Disposables = new List<IDisposable>();
        
        public static void Start()
        {
            RewardGold();
            CreateNovice();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            Ranking();
        }

        public static void Stop()
        {
            Disposables.DisposeAllAndClear();
        }
        
        private static void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == States.AgentState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    ReactiveAgentState.Gold.Value = States.AgentState.Value.gold += eval.Action.gold;
                }).AddTo(Disposables);
        }

        /// <summary>
        /// ToDo. 지금은 아바타가 사이닝하지만, 액션의 주체인 에이전트가 사이닝하게 바꾸는 것이 좋겠음.
        /// </summary>
        private static void CreateNovice()
        {
            ActionBase.EveryRender<CreateNovice>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var index = eval.Action.index;
                    var avatarAddress = AvatarManager.GetOrCreateAvatarAddress(index);
                    States.AgentState.Value.avatarAddresses.Add(index, avatarAddress);
                    States.AvatarStates.Add(index, (AvatarState) AgentController.Agent.GetState(avatarAddress));
                }).AddTo(Disposables);
        }
        
        private static void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var state = (AvatarState) AgentController.Agent.GetState(States.CurrentAvatarState.Value.address);
                    foreach (var item in States.AvatarStates)
                    {
                        if (item.Value.address != state.address)
                        {
                            continue;
                        }
                        
                        States.AvatarStates[item.Key] = state;
                        break;
                    }
                    
                    ReactiveCurrentAvatarState.AvatarState.Value = States.CurrentAvatarState.Value = state;
                }).AddTo(Disposables);
        }

        private static void Combination()
        {
            ActionBase.EveryRender<Combination>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    //
                }).AddTo(Disposables);
        }

        private static void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Register(ReactiveShopState.Items, States.CurrentAvatarState.Value.address,
                        result.shopItem);
                }).AddTo(Disposables);
        }

        private static void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(Disposables);
        }

        private static void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(Disposables);
        }

        private static void Ranking()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var state = (RankingState) AgentController.Agent.GetState(RankingState.Address);
                    ReactiveRankingState.RankingState.Value = States.RankingState.Value = state;
                })
                .AddTo(Disposables);
        }
    }
}
