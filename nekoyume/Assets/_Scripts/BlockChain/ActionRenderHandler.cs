using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
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
            CreateNovice();
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
        
        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == States.Instance.agentState.Value.address)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    ReactiveAgentState.Gold.Value = States.Instance.agentState.Value.gold += eval.Action.gold;
                }).AddTo(_disposables);
        }

        private void CreateNovice()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(eval => eval.InputContext.Signer == States.Instance.agentState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var index = eval.Action.index;
                    var avatarAddress = AvatarManager.GetOrCreateAvatarAddress(index);
                    States.Instance.agentState.Value.avatarAddresses.Add(index, avatarAddress);
                    States.Instance.avatarStates.Add(index, (AvatarState) AgentController.Agent.GetState(avatarAddress));
                }).AddTo(_disposables);
        }
        
        private void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
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
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    foreach (var material in eval.Action.Materials)
                    {
                        States.Instance.currentAvatarState.Value.RemoveItemFromItems(material.id, material.count);   
                    }
                    
                    var result = eval.Action.Result;
                    States.Instance.currentAvatarState.Value.AddEquipmentItemToItems(result.Item.id, result.Item.count);
                }).AddTo(_disposables);
        }

        private void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    States.Instance.currentAvatarState.Value.RemoveEquipmentItemFromItems(result.shopItem.item.Data.id, result.shopItem.count);
                    ShopState.Register(ReactiveShopState.Items, States.Instance.currentAvatarState.Value.address,
                        result.shopItem);
                }).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    States.Instance.currentAvatarState.Value.AddEquipmentItemToItems(result.shopItem.item.Data.id, result.shopItem.count);
                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(_disposables);
        }

        private void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    States.Instance.currentAvatarState.Value.AddEquipmentItemToItems(result.shopItem.item.Data.id, result.shopItem.count);
                    ShopState.Unregister(ReactiveShopState.Items, result.owner, result.shopItem.productId);
                }).AddTo(_disposables);
        }

        private void Ranking()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(eval => eval.InputContext.Signer == States.Instance.currentAvatarState.Value.address
                               && eval.Action.errorCode == GameAction.ErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var state = (RankingState) AgentController.Agent.GetState(RankingState.Address);
                    ReactiveRankingState.RankingState.Value = States.Instance.rankingState.Value = state;
                })
                .AddTo(_disposables);
        }
    }
}
