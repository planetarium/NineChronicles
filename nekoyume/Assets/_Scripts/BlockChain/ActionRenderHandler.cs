using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.Model.State;
using Nekoyume.Model.Quest;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Nekoyume.BlockChain
{
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using UniRx;

    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    /// </summary>
    public class ActionRenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        // FIXME We should move this constant to `StageSimulator.VersionV100025`
        private const int StageSimulatorVersionV100025 = 2;

        public static ActionRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private ActionRenderer _renderer;

        private IDisposable _disposableForBattleEnd = null;

        private ActionRenderHandler()
        {
        }

        public void Start(ActionRenderer renderer)
        {
            _renderer = renderer;

            RewardGold();
            GameConfig();
            CreateAvatar();
            TransferAsset();

            // Battle
            HackAndSlash();
            RankingBattle();
            MimisbrunnrBattle();

            // Craft
            CombinationConsumable();
            CombinationEquipment();
            ItemEnhancement();
            RapidCombination();

            // Market
            Sell();
            SellCancellation();
            UpdateSell();
            Buy();

            // Consume
            DailyReward();
            RedeemCode();
            ChargeActionPoint();
            ClaimMonsterCollectionReward();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void RewardGold()
        {
            // FIXME RewardGold의 결과(ActionEvaluation)에서 다른 갱신 주소가 같이 나오고 있는데 더 조사해봐야 합니다.
            // 우선은 HasUpdatedAssetsForCurrentAgent로 다르게 검사해서 우회합니다.
            _renderer.EveryRender<RewardGold>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState)
                .AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            _renderer.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            _renderer.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlash).AddTo(_disposables);
        }

        private void MimisbrunnrBattle()
        {
            _renderer.EveryRender<MimisbrunnrBattle>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseMimisbrunnr).AddTo(_disposables);
        }

        private void CombinationConsumable()
        {
            _renderer.EveryRender<CombinationConsumable>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationConsumable).AddTo(_disposables);
        }

        private void Sell()
        {
            _renderer.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            _renderer.EveryRender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation).AddTo(_disposables);
        }

        private void UpdateSell()
        {
            _renderer.EveryRender<UpdateSell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseUpdateSell).AddTo(_disposables);
        }

        private void Buy()
        {
            _renderer.EveryRender<Buy>()
                .ObserveOnMainThread()
                .Subscribe(ResponseBuy).AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _renderer.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement).AddTo(_disposables);
        }

        private void DailyReward()
        {
            _renderer.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyReward).AddTo(_disposables);
        }

        private void RankingBattle()
        {
            _renderer.EveryRender<RankingBattle>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRankingBattle).AddTo(_disposables);
        }

        private void CombinationEquipment()
        {
            _renderer.EveryRender<CombinationEquipment>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationEquipment).AddTo(_disposables);
        }

        private void RapidCombination()
        {
            _renderer.EveryRender<RapidCombination>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRapidCombination).AddTo(_disposables);
        }

        private void GameConfig()
        {
            _renderer.EveryRender(GameConfigState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateGameConfigState).AddTo(_disposables);
        }

        private void RedeemCode()
        {
            _renderer.EveryRender<Action.RedeemCode>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRedeemCode).AddTo(_disposables);
        }

        private void ChargeActionPoint()
        {
            _renderer.EveryRender<ChargeActionPoint>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseChargeActionPoint).AddTo(_disposables);
        }

        private void ClaimMonsterCollectionReward()
        {
            _renderer.EveryRender<ClaimMonsterCollectionReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimMonsterCollectionReward).AddTo(_disposables);
        }

        private void TransferAsset()
        {
            _renderer.EveryRender<TransferAsset>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAsset).AddTo(_disposables);
        }

        private void ResponseRapidCombination(ActionBase.ActionEvaluation<RapidCombination> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slotState = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (RapidCombination.ResultModel) slotState.Result;
                foreach (var pair in result.cost)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                var formatKey = string.Empty;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var combinationSlotState = States.Instance.GetCombinationSlotState(currentBlockIndex);
                var stateResult = combinationSlotState[slotIndex]?.Result;
                switch (stateResult)
                {
                    case CombinationConsumable5.ResultModel combineResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, combineResultModel.id,
                            currentBlockIndex);
                        if (combineResultModel.itemUsable is Equipment equipment)
                        {
                            if (combineResultModel.subRecipeId.HasValue &&
                                Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                                    combineResultModel.subRecipeId.Value,
                                    out var subRecipeRow))
                            {
                                formatKey = equipment.optionCountFromCombination == subRecipeRow.Options.Count
                                    ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                                    : "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                            else
                            {
                                formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                        }
                        else
                        {
                            formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        }

                        break;
                    }
                    case ItemEnhancement.ResultModel enhancementResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, enhancementResultModel.id,
                            currentBlockIndex);

                        switch (enhancementResultModel.enhancementResult)
                        {
                            case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Success:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Fail:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                                break;
                            default:
                                Debug.LogError(
                                    $"Unexpected result.enhancementResult: {enhancementResultModel.enhancementResult}");
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                        }

                        break;
                    }
                    default:
                        Debug.LogError(
                            $"Unexpected state.Result: {stateResult}");
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                UI.Notification.CancelReserve(result.itemUsable.TradableId);
                UI.Notification.Push(MailType.Workshop, string.Format(format, result.itemUsable.GetLocalizedName()));

                States.Instance.UpdateCombinationSlotState(slotIndex, slotState);
                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
            }
            Widget.Find<CombinationSlots>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseCombinationEquipment(ActionBase.ActionEvaluation<CombinationEquipment> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel) slot.Result;

                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(avatarAddress, result.itemUsable.ItemId, result.itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                var gameInstance = Game.Game.instance;
                var nextQuest = avatarState.questList?
                    .OfType<CombinationEquipmentQuest>()
                    .Where(x => !x.Complete)
                    .OrderBy(x => x.StageId)
                    .FirstOrDefault(x =>
                        gameInstance.TableSheets.EquipmentItemRecipeSheet.TryGetValue(x.RecipeId, out _));

                States.Instance.UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                RenderQuest(avatarAddress, avatarState.questList?.completedQuestIds);

                if (!(nextQuest is null))
                {
                    var isRecipeMatch = nextQuest.RecipeId == eval.Action.recipeId;

                    if (isRecipeMatch)
                    {
                        var celebratesPopup = Widget.Find<CelebratesPopup>();
                        celebratesPopup.Show(nextQuest);
                        celebratesPopup.OnDisableObservable
                            .First()
                            .Subscribe(_ =>
                            {
                                var menu = Widget.Find<Menu>();
                                if (menu.isActiveAndEnabled)
                                {
                                    menu.UpdateGuideQuest(avatarState);
                                }

                                var combination = Widget.Find<Combination>();
                                if (combination.isActiveAndEnabled)
                                {
                                    combination.UpdateRecipe();
                                }
                            });
                    }
                }

                // Notify
                string formatKey;
                if (result.itemUsable is Equipment equipment)
                {
                    if (eval.Action.subRecipeId.HasValue &&
                        Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                            eval.Action.subRecipeId.Value,
                            out var row))
                    {
                        formatKey = equipment.optionCountFromCombination == row.Options.Count
                            ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                            : "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                    else
                    {
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                }
                else
                {
                    Debug.LogError($"[{nameof(ResponseCombinationEquipment)}] result.itemUsable is not Equipment");
                    formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                }

                var format = L10nManager.Localize(formatKey);
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }
            Widget.Find<CombinationSlots>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseCombinationConsumable(ActionBase.ActionEvaluation<CombinationConsumable> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel) slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId, itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                States.Instance.UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }
            Widget.Find<CombinationSlots>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (ItemEnhancement.ResultModel) slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.AddItem(avatarAddress, itemUsable.TradableId, itemUsable.RequiredBlockIndex, 1);
                foreach (var tradableId in result.materialItemIdList)
                {
                    if (avatarState.inventory.TryGetNonFungibleItem(tradableId,
                        out ItemUsable materialItem))
                    {
                        LocalLayerModifier.AddItem(avatarAddress, tradableId, materialItem.RequiredBlockIndex, 1);
                    }
                }

                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.TradableId, itemUsable.RequiredBlockIndex, 1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                States.Instance.UpdateCombinationSlotState(slotIndex, slot);
                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                string formatKey;
                switch (result.enhancementResult)
                {
                    case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Success:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Fail:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                        break;
                    default:
                        Debug.LogError($"Unexpected result.enhancementResult: {result.enhancementResult}");
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.TradableId);
                // ~Notify
            }

            Widget.Find<CombinationSlots>().SetCaching(eval.Action.slotIndex, false);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (eval.Exception is null)
            {
                var count = eval.Action.count;
                var outputStates = eval.OutputStates;
                var item = GetItem(outputStates, eval.Action.tradableId);
                if (item is null)
                {
                    return;
                }

                string message = string.Empty;
                if (count > 1)
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_COMPLETE"),
                        item.GetLocalizedName(),
                        count);
                }
                else
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_COMPLETE"),
                        item.GetLocalizedName());
                }

                OneLinePopup.Push(MailType.Auction, message);

                UpdateCurrentAvatarState(eval);
                var shopSell = Widget.Find<ShopSell>();
                if (shopSell.isActiveAndEnabled)
                {
                    shopSell.Refresh();
                }
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
            var itemName = Util.GetItemNameByOrdierId(order.OrderId);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            LocalLayerModifier.RemoveItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
            LocalLayerModifier.AddNewMail(avatarAddress, eval.Action.orderId);
            var format = L10nManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
            OneLinePopup.Push(MailType.Auction, string.Format(format, itemName));
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

            var itemName = Util.GetItemNameByOrdierId(eval.Action.orderId);
            var format = L10nManager.Localize("NOTIFICATION_REREGISTER_COMPLETE");
            OneLinePopup.Push(MailType.Auction, string.Format(format, itemName));
            UpdateCurrentAvatarState(eval);
            var shopSell = Widget.Find<ShopSell>();
            if (shopSell.isActiveAndEnabled)
            {
                shopSell.Refresh();
            }
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (!(eval.Exception is null))
            {
                Debug.Log(eval.Exception);
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
                    var order = Util.GetOrder(purchaseInfo.OrderId);
                    var itemName = Util.GetItemNameByOrdierId(order.OrderId);
                    var price = purchaseInfo.Price;

                    if (errors.Exists(tuple => tuple.orderId.Equals(purchaseInfo.OrderId)))
                    {
                        var (orderId, errorCode) =
                            errors.FirstOrDefault(tuple => tuple.orderId == purchaseInfo.OrderId);

                        var errorType = ((ShopErrorType) errorCode).ToString();
                        LocalLayerModifier.ModifyAgentGold(agentAddress, price);
                        var msg = string.Format(L10nManager.Localize("NOTIFICATION_BUY_FAIL"),
                            itemName,
                            L10nManager.Localize(errorType),
                            price);
                        OneLinePopup.Push(MailType.Auction, msg);
                    }
                    else
                    {
                        var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                        LocalLayerModifier.ModifyAgentGold(agentAddress, price);
                        LocalLayerModifier.RemoveItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
                        LocalLayerModifier.AddNewMail(avatarAddress, purchaseInfo.OrderId);

                        var format = L10nManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                        OneLinePopup.Push(MailType.Auction, string.Format(format, itemName, price));
                    }
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
                    const string nameWithHashFormat = "{0} <size=80%><color=#A68F7E>#{1}</color></size>";
                    var buyerNameWithHash = string.Format(
                        nameWithHashFormat,
                        ((Text) ((Dictionary) buyerAvatarStateValue)["name"]).Value,
                        eval.Action.buyerAvatarAddress.ToHex().Substring(0, 4)
                    );

                    var order = Util.GetOrder(purchaseInfo.OrderId);
                    var itemName = Util.GetItemNameByOrdierId(order.OrderId);
                    var taxedPrice = order.Price - order.GetTax();

                    LocalLayerModifier.ModifyAgentGold(agentAddress, -taxedPrice);
                    LocalLayerModifier.AddNewMail(avatarAddress, purchaseInfo.OrderId);

                    var message = string.Format(
                        L10nManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE"),
                        buyerNameWithHash,
                        itemName);
                    OneLinePopup.Push(MailType.Auction, message);
                }
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseDailyReward(ActionBase.ActionEvaluation<DailyReward> eval)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
            }

            if (eval.Exception is null &&
                eval.Action.avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                LocalLayer.Instance.ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                    eval.Action.avatarAddress);
                UpdateCurrentAvatarState(eval);
                UI.Notification.Push(MailType.System, L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            }
        }

        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarState(eval);
                                UpdateWeeklyArenaState(eval);
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(eval.Action.avatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var simulator = new StageSimulator(
                    new LocalRandom(eval.RandomSeed),
                    States.Instance.CurrentAvatarState,
                    eval.Action.foods,
                    eval.Action.worldId,
                    eval.Action.stageId,
                    Game.Game.instance.TableSheets.GetStageSimulatorSheets(),
                    Game.Game.instance.TableSheets.CostumeStatSheet,
                    StageSimulatorVersionV100025
                );
                simulator.Simulate();
                var log = simulator.Log;


                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<QuestPreparation>().IsActive())
                    {
                        Widget.Find<QuestPreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingScreen>().IsActive() &&
                         Widget.Find<BattleResult>().IsActive())
                {
                    Widget.Find<BattleResult>().NextStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingScreen>().IsActive())
                {
                    Widget.Find<StageLoadingScreen>().Close();
                }
                if (Widget.Find<BattleResult>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResult>().Close();
                }

                var exc = eval.Exception.InnerException;
                BackToMain(showLoadingScreen, exc);
            }
        }

        private void ResponseMimisbrunnr(ActionBase.ActionEvaluation<MimisbrunnrBattle> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarState(eval);
                                UpdateWeeklyArenaState(eval);
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(eval.Action.avatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });
                var simulator = new StageSimulator(
                    new LocalRandom(eval.RandomSeed),
                    States.Instance.CurrentAvatarState,
                    eval.Action.foods,
                    eval.Action.worldId,
                    eval.Action.stageId,
                    Game.Game.instance.TableSheets.GetStageSimulatorSheets(),
                    Game.Game.instance.TableSheets.CostumeStatSheet,
                    StageSimulatorVersionV100025
                );
                simulator.Simulate();
                BattleLog log = simulator.Log;

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<MimisbrunnrPreparation>().IsActive())
                    {
                        Widget.Find<MimisbrunnrPreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingScreen>().IsActive() &&
                         Widget.Find<BattleResult>().IsActive())
                {
                    Widget.Find<BattleResult>().NextMimisbrunnrStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingScreen>().IsActive())
                {
                    Widget.Find<StageLoadingScreen>().Close();
                }
                if (Widget.Find<BattleResult>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResult>().Close();
                }

                var exc = eval.Exception.InnerException;
                BackToMain(showLoadingScreen, exc);
            }
        }

        private void ResponseRankingBattle(ActionBase.ActionEvaluation<RankingBattle> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateAgentState(eval);
                                UpdateCurrentAvatarState(eval);
                                UpdateWeeklyArenaState(eval);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
                {
                    Widget.Find<RankingBoard>().GoToStage(eval.Action.Result);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
                {
                    Widget.Find<ArenaBattleLoadingScreen>().Close();
                }
                if (Widget.Find<RankingBattleResult>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<RankingBattleResult>().Close();
                }

                BackToMain(showLoadingScreen, eval.Exception.InnerException);
            }
        }

        private void ResponseRedeemCode(ActionBase.ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            if (eval.Exception is null)
            {
                Widget.Find<CodeReward>().Show(eval.OutputStates.GetRedeemCodeState());
                key = "UI_REDEEM_CODE_SUCCESS";
                UpdateCurrentAvatarState(eval);
            }
            else
            {
                if (eval.Exception.InnerException is DuplicateRedeemException)
                {
                    key = "UI_REDEEM_CODE_ALREADY_USE";
                }
            }

            var msg = L10nManager.Localize(key);
            UI.Notification.Push(MailType.System, msg);
        }

        private void ResponseChargeActionPoint(ActionBase.ActionEvaluation<ChargeActionPoint> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -States.Instance.GameConfigState.ActionPointMax);
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, 1);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
                }

                UpdateCurrentAvatarState(eval);
            }
        }

        private void ResponseClaimMonsterCollectionReward(ActionBase.ActionEvaluation<ClaimMonsterCollectionReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            if (!eval.OutputStates.TryGetAvatarStateV2(agentAddress, avatarAddress, out var avatarState))
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
                    LocalLayerModifier.RemoveItem(avatarAddress,
                                                  tradableId,
                                                  tradableItem.RequiredBlockIndex,
                                                  rewardInfo.Quantity);
                }
            }

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, mail.id);
            // ~LocalLayer

            // Notification
            UI.Notification.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"));

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
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
            var playToEarnRewardAddress = new Address("d595f7e85e1757d6558e9e448fa9af77ab28be4c");

            if (senderAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;
                var messageFormat = L10nManager.Localize("UI_TRANSFERASSET_NOTIFICATION_SENDER");
                var message = string.Format(messageFormat, amount, recipientAddress);

                OneLinePopup.Push(MailType.System, message);
            }
            else if (recipientAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;
                string message;
                if (senderAddress == playToEarnRewardAddress)
                {
                    var messageFormat = L10nManager.Localize("UI_PLAYTOEARN_NOTIFICATION_FORMAT");
                    message = string.Format(messageFormat, amount);
                }
                else
                {
                    var messageFormat = L10nManager.Localize("UI_TRANSFERASSET_NOTIFICATION_RECIPIENT");
                    message = string.Format(messageFormat, amount, senderAddress);
                }

                OneLinePopup.Push(MailType.System, message);
            }
            UpdateAgentState(eval);
        }

        public static void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            if (avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var questList = States.Instance.CurrentAvatarState.questList;
            foreach (var id in ids)
            {
                var quest = questList.First(q => q.Id == id);
                var rewardMap = quest.Reward.ItemMap;

                foreach (var reward in rewardMap)
                {
                    var materialRow = Game.Game.instance.TableSheets
                        .MaterialItemSheet
                        .First(pair => pair.Key == reward.Key);

                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        materialRow.Value.ItemId,
                        reward.Value);
                }

                LocalLayerModifier.AddReceivableQuest(avatarAddress, id);
            }
        }

        public static void BackToMain(bool showLoadingScreen, Exception exc)
        {
            Debug.LogException(exc);

            var (key, code, errorMsg) = ErrorCode.GetErrorCode(exc);
            Game.Event.OnRoomEnter.Invoke(showLoadingScreen);
            Game.Game.instance.Stage.OnRoomEnterEnd
                .First()
                .Subscribe(_ => PopupError(key, code, errorMsg));

            MainCanvas.instance.InitWidgetInMain();
        }

        public static void PopupError(Exception exc)
        {
            Debug.LogException(exc);
            var (key, code, errorMsg) = ErrorCode.GetErrorCode(exc);
            PopupError(key, code, errorMsg);
        }

        private static void PopupError(string key, string code, string errorMsg)
        {
            errorMsg = errorMsg == string.Empty
                ? string.Format(
                    L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                    L10nManager.Localize(key),
                    code)
                : errorMsg;
            Widget
                .Find<SystemPopup>()
                .Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                    L10nManager.Localize("UI_OK"), false);
        }

        private static ItemBase GetItem(IAccountStateDelta state, Guid tradableId)
        {
            var address = Addresses.GetItemAddress(tradableId);
            if (state.GetState(address) is Dictionary dictionary)
            {
                return ItemFactory.Deserialize(dictionary);
            }

            return null;
        }

        private class LocalRandom : System.Random, IRandom
        {
            public LocalRandom(int Seed)
                : base(Seed)
            {
            }

            public int Seed => throw new NotImplementedException();
        }
    }
}
