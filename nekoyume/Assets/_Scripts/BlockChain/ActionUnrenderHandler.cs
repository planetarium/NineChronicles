using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderer;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.State.Subjects;
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
            BuyMultiple();
            Sell();
            SellCancellation();
            DailyReward();
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
                        Debug.Log($"Action unrender: {nameof(RewardGold)} | {e}");
                    }

                    UpdateAgentState(eval);
                })
                .AddTo(_disposables);
        }

        private void BuyMultiple()
        {
            _renderer.EveryUnrender<BuyMultiple>()
                .ObserveOnMainThread()
                .Subscribe(ResponseBuyMultiple)
                .AddTo(_disposables);
        }

        private void Sell()
        {
            _renderer.EveryUnrender<Sell3>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell)
                .AddTo(_disposables);
        }

        private void SellCancellation()
        {
            _renderer.EveryUnrender<SellCancellation4>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation)
                .AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _renderer.EveryUnrender<ItemEnhancement5>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnrenderItemEnhancement)
                .AddTo(_disposables);
        }

        private void DailyReward()
        {
            _renderer.EveryUnrender<DailyReward3>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyReward)
                .AddTo(_disposables);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy4> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
            var price = eval.Action.sellerResult.shopItem.Price;
            Address renderQuestAvatarAddress;
            List<int> renderQuestCompletedQuestIds;

            if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                var buyerAgentAddress = States.Instance.AgentState.address;
                var result = eval.Action.buyerResult;

                var itemId = result.itemUsable?.ItemId ?? result.costume.ItemId;
                var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -price);
                LocalLayerModifier.AddItem(buyerAvatarAddress, itemId);
                LocalLayerModifier.RemoveNewAttachmentMail(buyerAvatarAddress, result.id);

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

                LocalLayerModifier.ModifyAgentGold(sellerAgentAddress, gold);
                LocalLayerModifier.RemoveNewAttachmentMail(sellerAvatarAddress, result.id);

                renderQuestAvatarAddress = sellerAvatarAddress;
                renderQuestCompletedQuestIds = sellerAvatar.questList.completedQuestIds;
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UnrenderQuest(renderQuestAvatarAddress, renderQuestCompletedQuestIds);
        }


        private void ResponseBuyMultiple(ActionBase.ActionEvaluation<BuyMultiple> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
            Address renderQuestAvatarAddress;
            var renderQuestCompletedQuestIds = new List<int>();

            if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                var purchaseResults = eval.Action.buyerResult.purchaseResults;
                foreach (var purchaseResult in purchaseResults)
                {
                    var buyerAgentAddress = States.Instance.AgentState.address;
                    var price = purchaseResult.shopItem.Price;
                    var itemId = purchaseResult.itemUsable?.ItemId ?? purchaseResult.costume.ItemId;
                    var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                    LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -price);
                    LocalLayerModifier.AddItem(buyerAvatarAddress, itemId);
                    LocalLayerModifier.RemoveNewAttachmentMail(buyerAvatarAddress, purchaseResult.id);

                    renderQuestAvatarAddress = buyerAvatarAddress;
                    renderQuestCompletedQuestIds = buyerAvatar.questList.completedQuestIds;
                }
            }
            else
            {
                foreach (var sellerResult in eval.Action.sellerResult.sellerResults)
                {
                    var purchaseInfos = eval.Action.purchaseInfos;
                    var purchaseInfo = purchaseInfos.FirstOrDefault(x => x.productId == sellerResult.id);
                    if (purchaseInfo != null)
                    {
                        var sellerAvatarAddress = purchaseInfo.sellerAvatarAddress;
                        var sellerAgentAddress = purchaseInfo.sellerAgentAddress;
                        var gold = sellerResult.gold;
                        var sellerAvatar = eval.OutputStates.GetAvatarState(sellerAvatarAddress);

                        LocalLayerModifier.ModifyAgentGold(sellerAgentAddress, gold);
                        LocalLayerModifier.RemoveNewAttachmentMail(sellerAvatarAddress, sellerResult.id);

                        renderQuestAvatarAddress = sellerAvatarAddress;
                        renderQuestCompletedQuestIds = sellerAvatar.questList.completedQuestIds;
                    }
                }
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UnrenderQuest(renderQuestAvatarAddress, renderQuestCompletedQuestIds);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell3> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.sellerAvatarAddress;
            var itemId = eval.Action.itemId;

            LocalLayerModifier.RemoveItem(avatarAddress, itemId);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation4> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var result = eval.Action.result;
            var nonFungibleItem = result.itemUsable ?? (INonFungibleItem) result.costume;
            var avatarAddress = eval.Action.sellerAvatarAddress;
            var itemId = nonFungibleItem.ItemId;

            LocalLayerModifier.AddItem(avatarAddress, itemId);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseDailyReward(ActionBase.ActionEvaluation<DailyReward3> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.avatarAddress;
            var itemId = eval.Action.dailyRewardResult.materials.First().Key.ItemId;
            var itemCount = eval.Action.dailyRewardResult.materials.First().Value;
            LocalLayerModifier.AddItem(avatarAddress, itemId, itemCount);
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);
            ReactiveAvatarState.DailyRewardReceivedIndex.SetValueAndForceNotify(
                avatarState.dailyRewardReceivedIndex);
            GameConfigStateSubject.IsChargingActionPoint.SetValueAndForceNotify(false);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseUnrenderItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement5> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (ItemEnhancement.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            // NOTE: 사용한 자원에 대한 레이어 다시 추가하기.
            LocalLayerModifier.ModifyAgentGold(agentAddress, -result.gold);
            LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
            foreach (var itemId in result.materialItemIdList)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalLayerModifier.RemoveItem(avatarAddress, itemId);
            }

            // NOTE: 메일 레이어 다시 없애기.
            LocalLayerModifier.AddItem(avatarAddress, itemUsable.ItemId, false);
            LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, result.id);

            // NOTE: 워크샵 슬롯의 모든 휘발성 상태 변경자를 다시 추가하기.
            var otherItemId = result.materialItemIdList.First();
            LocalLayerModifier.ModifyCombinationSlotItemEnhancement(
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
                LocalLayerModifier.RemoveReceivableQuest(avatarAddress, id);

                var currentAvatarState = States.Instance.CurrentAvatarState;
                if (currentAvatarState.address != avatarAddress)
                {
                    continue;
                }
            }
        }
    }
}
