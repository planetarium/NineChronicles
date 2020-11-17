using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderer;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.State;
using Nekoyume.UI;
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

        private ActionUnrenderHandler()
        {
        }

        public void Start(ActionRenderer renderer)
        {
            _renderer = renderer;

            RewardGold();
            Buy();
            Sell();
            ItemEnhancement();
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
                        Debug.Log($"Action unrender: {nameof(RewardGold)} | failed to get GoldBalanceState");
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

        private void ItemEnhancement()
        {
            _renderer.EveryUnrender<ItemEnhancement3>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnrenderItemEnhancement)
                .AddTo(_disposables);
        }
        
        private void ResponseUnrenderBuy(ActionBase.ActionEvaluation<Buy3> eval)
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

        private void ResponseUnrenderItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement3> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (ItemEnhancement.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            // NOTE: 사용한 자원에 대한 레이어 다시 추가하기.
            LocalStateModifier.ModifyAgentGold(agentAddress, -result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -result.actionPoint);
            LocalStateModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
            foreach (var itemId in result.materialItemIdList)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.RemoveItem(avatarAddress, itemId);
            }

            // NOTE: 메일 레이어 다시 없애기.
            LocalStateModifier.AddItem(avatarAddress, itemUsable.ItemId, false);
            LocalStateModifier.RemoveNewAttachmentMail(avatarAddress, result.id);

            // NOTE: 워크샵 슬롯의 모든 휘발성 상태 변경자를 다시 추가하기.
            var otherItemId = result.materialItemIdList.First();
            LocalStateModifier.ModifyCombinationSlotItemEnhancement(
                itemUsable.ItemId,
                otherItemId,
                eval.Action.slotIndex);

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UpdateCombinationSlotState(slot);
            UnrenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
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
