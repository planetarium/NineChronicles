using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderer;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public class ActionUnrenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionUnrenderHandler Value = new ActionUnrenderHandler();
        }

        public static readonly ActionUnrenderHandler Instance = Singleton.Value;

        private ActionRenderer _renderer;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Start(ActionRenderer renderer)
        {
            _renderer = renderer;

            RewardGold();
            Buy();
            Sell();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void RewardGold()
        {
            _renderer.EveryUnrender<RewardGold>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(onNext: eval =>
                {
                    // NOTE: 잘 들어오는지 확인하기 위해서 당분간 로그를 남깁니다.(2020.11.02)
                    try
                    {
                        var goldBalanceState = GetGoldBalanceState(eval);
                        Debug.Log($"Action unrender: {nameof(RewardGold)} | gold: {goldBalanceState.Gold}");
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Action unrender: {nameof(RewardGold)} | {e}");
                    }

                    UpdateAgentState(eval);
                })
                .AddTo(_disposables);
        }

        private void Buy()
        {
            _renderer.EveryUnrender<Buy>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnrenderBuy)
                .AddTo(_disposables);
        }

        private void Sell()
        {
            _renderer.EveryUnrender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnrenderSell)
                .AddTo(_disposables);
        }

        private void ResponseUnrenderBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
            var price = eval.Action.sellerResult.shopItem.Price;
            Address renderQuestAvatarAddress;
            List<int> renderQuestCompletedQuestIds;

            if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                var buyerAgentAddress = States.Instance.AgentState.address;
                var result = eval.Action.buyerResult;
                var itemId = result.itemUsable.ItemId;
                var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                LocalStateModifier.ModifyAgentGold(buyerAgentAddress, -price);
                LocalStateModifier.AddItem(buyerAvatarAddress, itemId);
                LocalStateModifier.RemoveNewAttachmentMail(buyerAvatarAddress, result.id);

                renderQuestAvatarAddress = buyerAvatarAddress;
                renderQuestCompletedQuestIds = buyerAvatar.questList.completedQuestIds;
            }
            else
            {
                var sellerAvatarAddress = eval.Action.sellerAvatarAddress;
                var sellerAgentAddress = eval.Action.sellerAgentAddress;
                var result = eval.Action.sellerResult;
                var gold = result.gold;
                var sellerAvatar = eval.OutputStates.GetAvatarState(sellerAvatarAddress);

                LocalStateModifier.ModifyAgentGold(sellerAgentAddress, gold);
                LocalStateModifier.RemoveNewAttachmentMail(sellerAvatarAddress, result.id);

                renderQuestAvatarAddress = sellerAvatarAddress;
                renderQuestCompletedQuestIds = sellerAvatar.questList.completedQuestIds;
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UnrenderQuest(renderQuestAvatarAddress, renderQuestCompletedQuestIds);
        }

        private void ResponseUnrenderSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.sellerAvatarAddress;
            var itemId = eval.Action.itemId;

            LocalStateModifier.RemoveItem(avatarAddress, itemId);
            UpdateCurrentAvatarState(eval);
        }

        public void UnrenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                LocalStateModifier.RemoveReceivableQuest(avatarAddress, id);

                var currentAvatarState = States.Instance.CurrentAvatarState;
                if (currentAvatarState.address != avatarAddress)
                {
                    continue;
                }
            }
        }
    }
}
