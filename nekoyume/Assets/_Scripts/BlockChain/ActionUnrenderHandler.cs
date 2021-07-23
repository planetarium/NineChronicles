using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Order;
using Lib9c.Renderer;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI;
using UniRx;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    using UniRx;

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
            TransferAsset();
            // GameConfig(); todo.
            // CreateAvatar(); ignore.

            // Battle
            // HackAndSlash(); todo.
            // RankingBattle(); todo.
            // MimisbrunnrBattle(); todo.

            // Craft
            // CombinationConsumable(); todo.
            // CombinationEquipment(); todo.
            ItemEnhancement();
            // RapidCombination(); todo.

            // Market
            Sell();
            SellCancellation();
            UpdateSell();
            Buy();

            // Consume
            DailyReward();
            // RedeemCode(); todo.
            // ChargeActionPoint(); todo.
            ClaimMonsterCollectionReward();
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
                .ObserveOnMainThread()
                .Subscribe(ResponseBuy)
                .AddTo(_disposables);
        }

        private void Sell()
        {
            _renderer.EveryUnrender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell)
                .AddTo(_disposables);
        }

        private void SellCancellation()
        {
            _renderer.EveryUnrender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation)
                .AddTo(_disposables);
        }

        private void UpdateSell()
        {
            _renderer.EveryUnrender<UpdateSell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseUpdateSell)
                .AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _renderer.EveryUnrender<ItemEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnrenderItemEnhancement)
                .AddTo(_disposables);
        }

        private void DailyReward()
        {
            _renderer.EveryUnrender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyReward)
                .AddTo(_disposables);
        }

        private void ClaimMonsterCollectionReward()
        {
            _renderer.EveryUnrender<ClaimMonsterCollectionReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimMonsterCollectionReward)
                .AddTo(_disposables);
        }

        private void TransferAsset()
        {
            _renderer.EveryUnrender<TransferAsset>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAsset)
                .AddTo(_disposables);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress,
                out var avatarState))
            {
                return;
            }

            var errors = eval.Action.errors.ToList();
            var purchaseInfos = eval.Action.purchaseInfos;
            if (eval.Action.buyerAvatarAddress == avatarAddress) // buyer
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    if (errors.Exists(tuple => tuple.orderId.Equals(purchaseInfo.OrderId)))
                    {
                        continue;
                    }

                    var price = purchaseInfo.Price;
                    var order = Util.GetOrder(purchaseInfo.OrderId);
                    var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                    LocalLayerModifier.ModifyAgentGold(agentAddress, -price);
                    LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, purchaseInfo.OrderId);
                }
            }
            else // seller
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    var buyerAvatarStateValue = eval.OutputStates.GetState(eval.Action.buyerAvatarAddress);
                    if (buyerAvatarStateValue is null)
                    {
                        Debug.LogError("buyerAvatarStateValue is null.");
                        return;
                    }

                    var order = Util.GetOrder(purchaseInfo.OrderId);
                    var taxedPrice = order.Price - order.GetTax();
                    LocalLayerModifier.ModifyAgentGold(agentAddress, taxedPrice);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, purchaseInfo.OrderId);
                }
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UnrenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.sellerAvatarAddress;
            var itemId = eval.Action.tradableId;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var count = eval.Action.count;
            LocalLayerModifier.RemoveItem(avatarAddress, itemId, blockIndex, count);
            UpdateCurrentAvatarState(eval);
            var shopSell = Widget.Find<ShopSell>();
            if (shopSell.isActiveAndEnabled)
            {
                shopSell.Refresh();
            }
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.sellerAvatarAddress;
            var order = Util.GetOrder(eval.Action.orderId);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
            UpdateCurrentAvatarState(eval);
            var shopSell = Widget.Find<ShopSell>();
            if (shopSell.isActiveAndEnabled)
            {
                shopSell.Refresh();
            }
        }

        private void ResponseUpdateSell(ActionBase.ActionEvaluation<UpdateSell> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            UpdateCurrentAvatarState(eval);
            var shopSell = Widget.Find<ShopSell>();
            if (shopSell.isActiveAndEnabled)
            {
                shopSell.Refresh();
            }
        }


        private void ResponseDailyReward(ActionBase.ActionEvaluation<DailyReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            // This should be not inversed with `ActionRenderHandler`.
            // When DailyReward is unrendered, use of ActionPoint is cancelled. So loading indicator should not appear.
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(
                    eval.Action.avatarAddress);
            }

            if (eval.Action.avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = eval.Action.avatarAddress;
            var fungibleId = eval.Action.dailyRewardResult.materials.First().Key.ItemId;
            var itemCount = eval.Action.dailyRewardResult.materials.First().Value;
            LocalLayerModifier.AddItem(avatarAddress, fungibleId, itemCount);
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress,
                out var avatarState))
            {
                return;
            }
            ReactiveAvatarState.DailyRewardReceivedIndex.SetValueAndForceNotify(
                avatarState.dailyRewardReceivedIndex);

            UpdateCurrentAvatarState(avatarState);
        }

        private void ResponseUnrenderItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (ItemEnhancement.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress,
                out var avatarState))
            {
                return;
            }

            // NOTE: 사용한 자원에 대한 레이어 다시 추가하기.
            LocalLayerModifier.ModifyAgentGold(agentAddress, -result.gold);
            LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId, itemUsable.RequiredBlockIndex, 1);
            foreach (var itemId in result.materialItemIdList)
            {
                if (avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable materialItem))
                {
                    LocalLayerModifier.RemoveItem(avatarAddress, itemId, materialItem.RequiredBlockIndex, 1);
                }
            }

            // NOTE: 메일 레이어 다시 없애기.
            LocalLayerModifier.AddItem(avatarAddress, itemUsable.TradableId, itemUsable.RequiredBlockIndex, 1);
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

        private void ResponseClaimMonsterCollectionReward(ActionBase.ActionEvaluation<ClaimMonsterCollectionReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = eval.Action.avatarAddress;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress,
                out var avatarState))
            {
                return;
            }

            var mail = avatarState.mailBox.FirstOrDefault(e => e is MonsterCollectionMail);
            if (!(mail is MonsterCollectionMail {attachment: MonsterCollectionResult monsterCollectionResult}))
            {
                return;
            }

            // LocalLayer
            var rewardInfos = monsterCollectionResult.rewards;
            for (var i = 0; i < rewardInfos.Count; i++)
            {
                var rewardInfo = rewardInfos[i];
                if (!rewardInfo.ItemId.TryParseAsTradableId(
                    Game.Game.instance.TableSheets.ItemSheet,
                    out var tradableId))
                {
                    continue;
                }

                if (!rewardInfo.ItemId.TryGetFungibleId(
                    Game.Game.instance.TableSheets.ItemSheet,
                    out var fungibleId))
                {
                    continue;
                }

                avatarState.inventory.TryGetFungibleItems(fungibleId, out var items);
                var item = items.FirstOrDefault(x => x.item is ITradableItem);
                if (item != null && item is ITradableItem tradableItem)
                {
                    LocalLayerModifier.AddItem(avatarAddress, tradableId, tradableItem.RequiredBlockIndex, rewardInfo.Quantity);
                }
            }

            LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id);
            // ~LocalLayer

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UnrenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseTransferAsset(ActionBase.ActionEvaluation<TransferAsset> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var senderAddress = eval.Action.Sender;
            var recipientAddress = eval.Action.Recipient;
            var currentAgentAddress = States.Instance.AgentState.address;

            if (recipientAddress == currentAgentAddress ||
                senderAddress == currentAgentAddress)
            {
                UpdateAgentState(eval);
            }
        }

        public static void UnrenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            if (avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            foreach (var id in ids)
            {
                LocalLayerModifier.RemoveReceivableQuest(avatarAddress, id);
            }
        }
    }
}
