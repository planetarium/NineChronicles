using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using UniRx;
using Nekoyume.Model.State;
using TentuPlay.Api;
using Nekoyume.Model.Quest;
using Nekoyume.State.Modifiers;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.BlockChain
{
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
            CreateAvatar();
            HackAndSlash();
            MimisbrunnrBattle();
            CombinationConsumable();
            Sell();
            SellCancellation();
            Buy();
            DailyReward();
            ItemEnhancement();
            RankingBattle();
            CombinationEquipment();
            RapidCombination();
            GameConfig();
            RedeemCode();
            ChargeActionPoint();
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
                .Subscribe(eval =>
                {
                    //[TentuPlay] RewardGold 기록
                    //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                    Address agentAddress = States.Instance.AgentState.address;
                    if (eval.OutputStates.TryGetGoldBalance(agentAddress, GoldCurrency, out var balance))
                    {
                        new TPStashEvent().CharacterCurrencyGet(
                            player_uuid: agentAddress.ToHex(),
                            // FIXME: Sometimes `States.Instance.CurrentAvatarState` is null.
                            character_uuid: States.Instance.CurrentAvatarState?.address.ToHex().Substring(0, 4) ?? string.Empty,
                            currency_slug: "gold",
                            currency_quantity: float.Parse((balance - States.Instance.GoldBalanceState.Gold).GetQuantityString()),
                            currency_total_quantity: float.Parse(balance.GetQuantityString()),
                            reference_entity: entity.Bonuses,
                            reference_category_slug: "reward_gold",
                            reference_slug: "RewardGold");
                    }

                    UpdateAgentState(eval);

                }).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            _renderer.EveryRender<CreateAvatar2>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    //[TentuPlay] 캐릭터 획득
                    Address agentAddress = States.Instance.AgentState.address;
                    Address avatarAddress = agentAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CreateAvatar2.DeriveFormat,
                            eval.Action.index
                        )
                    );
                    new TPStashEvent().PlayerCharacterGet(
                        player_uuid: agentAddress.ToHex(),
                        character_uuid: avatarAddress.ToHex().Substring(0, 4),
                        characterarchetype_slug: Nekoyume.GameConfig.DefaultAvatarCharacterId.ToString(), //100010 for now.
                        //-> WARRIOR, ARCHER, MAGE, ACOLYTE를 구분할 수 있는 구분자여야한다.
                        reference_entity: entity.Etc,
                        reference_category_slug: null,
                        reference_slug: null
                    );

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

        private void Buy()
        {
            _renderer.EveryRender<Buy>()
                .Where(ValidateEvaluationForAgentState)

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
                .Subscribe(eval =>
                {
                    LocalLayer.Instance
                        .ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                            eval.Action.avatarAddress);

                    UpdateCurrentAvatarState(eval);

                    if (eval.Exception is null)
                    {
                        UI.Notification.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"));
                        var avatarAddress = eval.Action.avatarAddress;
                        var itemId = eval.Action.dailyRewardResult.materials.First().Key.ItemId;
                        var itemCount = eval.Action.dailyRewardResult.materials.First().Value;
                        LocalLayerModifier.RemoveItem(avatarAddress, itemId, itemCount);
                        LocalLayerModifier.AddNewAttachmentMail(avatarAddress, eval.Action.dailyRewardResult.id);
                        WidgetHandler.Instance.Menu.SetActiveActionPointLoading(false);
                    }

                }).AddTo(_disposables);
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
            _renderer.EveryRender<RapidCombination2>()
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

        private void ResponseRapidCombination(ActionBase.ActionEvaluation<RapidCombination2> eval)
        {
            var avatarAddress = eval.Action.avatarAddress;
            var slot =
                eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (RapidCombination.ResultModel) slot.Result;
            foreach (var pair in result.cost)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }
            LocalLayerModifier.RemoveAvatarItemRequiredIndex(avatarAddress, result.itemUsable.ItemId);
            LocalLayerModifier.ResetCombinationSlot(slot);

            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);

            //[TentuPlay] RapidCombinationConsumable 합성에 사용한 골드 기록
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            var agentAddress = eval.Signer;
            var qty = eval.OutputStates.GetAvatarState(avatarAddress).inventory.Materials
                .Count(i => i.ItemSubType == ItemSubType.Hourglass);
            var prevQty = eval.PreviousStates.GetAvatarState(avatarAddress).inventory.Materials
                .Count(i => i.ItemSubType == ItemSubType.Hourglass);
            new TPStashEvent().CharacterItemUse(
                player_uuid: agentAddress.ToHex(),
                character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                item_category: itemCategory.Consumable,
                item_slug: "hourglass",
                item_quantity: (float)(prevQty - qty),
                reference_entity: entity.Items,
                reference_category_slug: "consumables_rapid_combination",
                reference_slug: slot.Result.itemUsable.Id.ToString()
            );

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UpdateCombinationSlotState(slot);
        }

        private void ResponseCombinationEquipment(ActionBase.ActionEvaluation<CombinationEquipment> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.AvatarAddress;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.SlotIndex);
                var result = (CombinationConsumable.ResultModel) slot.Result;
                var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

                // NOTE: 사용한 자원에 대한 레이어 벗기기.
                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
                }

                // NOTE: 메일 레이어 씌우기.
                LocalLayerModifier.RemoveItem(avatarAddress, result.itemUsable.ItemId);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);
                LocalLayerModifier.ResetCombinationSlot(slot);

                // NOTE: 노티 예약 걸기.
                var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId);

                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);

                //[TentuPlay] Equipment 합성에 사용한 골드 기록
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                if (eval.OutputStates.TryGetGoldBalance(agentAddress, GoldCurrency, out var balance))
                {
                    var total = balance - new FungibleAssetValue(balance.Currency, result.gold, 0);
                    new TPStashEvent().CharacterCurrencyUse(
                        player_uuid: agentAddress.ToHex(),
                        character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                        currency_slug: "gold",
                        currency_quantity: (float) result.gold,
                        currency_total_quantity: float.Parse(total.GetQuantityString()),
                        reference_entity: entity.Items,
                        reference_category_slug: "equipments_combination",
                        reference_slug: result.itemUsable.Id.ToString());
                }

                var gameInstance = Game.Game.instance;

                var nextQuest = gameInstance.States.CurrentAvatarState.questList?
                    .OfType<CombinationEquipmentQuest>()
                    .Where(x => !x.Complete)
                    .OrderBy(x => x.StageId)
                    .FirstOrDefault(x =>
                        gameInstance.TableSheets.EquipmentItemRecipeSheet.TryGetValue(x.RecipeId, out _));

                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
                UpdateCombinationSlotState(slot);

                if (!(nextQuest is null))
                {
                    var isRecipeMatch = nextQuest.RecipeId == eval.Action.RecipeId;

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
            }
        }

        private void ResponseCombinationConsumable(ActionBase.ActionEvaluation<CombinationConsumable> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.AvatarAddress;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
                var result = (CombinationConsumable.ResultModel) slot.Result;
                var itemUsable = result.itemUsable;
                var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
                }

                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);
                LocalLayerModifier.ResetCombinationSlot(slot);

                var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId
                );
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);

                //[TentuPlay] Consumable 합성에 사용한 골드 기록
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                if (eval.OutputStates.TryGetGoldBalance(agentAddress, GoldCurrency, out var balance))
                {
                    var total = balance - new FungibleAssetValue(balance.Currency, result.gold, 0);
                    new TPStashEvent().CharacterCurrencyUse(
                        player_uuid: agentAddress.ToHex(),
                        character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                        currency_slug: "gold",
                        currency_quantity: (float)result.gold,
                        currency_total_quantity: float.Parse(total.GetQuantityString()),
                        reference_entity: entity.Items,
                        reference_category_slug: "consumables_combination",
                        reference_slug: result.itemUsable.Id.ToString());
                }

                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                UpdateCombinationSlotState(slot);
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
            }
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.sellerAvatarAddress;
                var itemId = eval.Action.itemId;

                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalLayerModifier.AddItem(avatarAddress, itemId, false);
                var format = L10nManager.Localize("NOTIFICATION_SELL_COMPLETE");
                var shopState = new ShopState((Dictionary) eval.OutputStates.GetState(ShopState.Address));

                var shopItem = shopState.Products.Values.First(r =>
                {
                    var nonFungibleItem = r.ItemUsable ?? (INonFungibleItem)r.Costume;
                    return nonFungibleItem.ItemId == itemId;
                });

                var itemBase = shopItem.ItemUsable ?? (ItemBase) shopItem.Costume;
                UI.Notification.Push(MailType.Auction, string.Format(format, itemBase.GetLocalizedName()));
                UpdateCurrentAvatarState(eval);
            }
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.sellerAvatarAddress;
                var result = eval.Action.result;
                var nonFungibleItem = result.itemUsable ?? (INonFungibleItem) result.costume;
                var itemBase = result.itemUsable ?? (ItemBase) result.costume;

                LocalLayerModifier.RemoveItem(avatarAddress, nonFungibleItem.ItemId);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);
                var format = L10nManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
                UI.Notification.Push(MailType.Auction, string.Format(format, itemBase.GetLocalizedName()));
                UpdateCurrentAvatarState(eval);
            }
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (eval.Exception is null)
            {
                var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
                var price = eval.Action.sellerResult.shopItem.Price;
                Address renderQuestAvatarAddress;
                List<int> renderQuestCompletedQuestIds = null;

                if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
                {
                    var buyerAgentAddress = States.Instance.AgentState.address;
                    var result = eval.Action.buyerResult;
                    var nonFungibleItem = result.itemUsable ?? (INonFungibleItem) result.costume;
                    var itemBase = result.itemUsable ?? (ItemBase) result.costume;
                    var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                    // 골드 처리.
                    LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, price);

                    // 메일 처리.
                    LocalLayerModifier.RemoveItem(buyerAvatarAddress, nonFungibleItem.ItemId);
                    LocalLayerModifier.AddNewAttachmentMail(buyerAvatarAddress, result.id);

                    var format = L10nManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                    UI.Notification.Push(MailType.Auction, string.Format(format, itemBase.GetLocalizedName()));

                    //[TentuPlay] 아이템 구입, 골드 사용
                    //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                    if (eval.OutputStates.TryGetGoldBalance(buyerAgentAddress, GoldCurrency, out var buyerAgentBalance))
                    {
                        var total = buyerAgentBalance - price;
                        new TPStashEvent().CharacterCurrencyUse(
                            player_uuid: States.Instance.AgentState.address.ToHex(),
                            character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                            currency_slug: "gold",
                            currency_quantity: float.Parse(price.GetQuantityString()),
                            currency_total_quantity: float.Parse(total.GetQuantityString()),
                            reference_entity: entity.Trades,
                            reference_category_slug: "buy",
                            reference_slug: itemBase.Id.ToString() //아이템 품번
                        );
                    }

                    renderQuestAvatarAddress = buyerAvatarAddress;
                    renderQuestCompletedQuestIds = buyerAvatar.questList.completedQuestIds;
                }
                else
                {
                    var sellerAvatarAddress = eval.Action.sellerAvatarAddress;
                    var sellerAgentAddress = eval.Action.sellerAgentAddress;
                    var result = eval.Action.sellerResult;
                    var itemBase = result.itemUsable ?? (ItemBase) result.costume;
                    var gold = result.gold;
                    var sellerAvatar = eval.OutputStates.GetAvatarState(sellerAvatarAddress);

                    LocalLayerModifier.ModifyAgentGold(sellerAgentAddress, -gold);
                    LocalLayerModifier.AddNewAttachmentMail(sellerAvatarAddress, result.id);

                    var format = L10nManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE");
                    var buyerName =
                        new AvatarState(
                                (Bencodex.Types.Dictionary) eval.OutputStates.GetState(eval.Action.buyerAvatarAddress))
                            .NameWithHash;
                    UI.Notification.Push(MailType.Auction, string.Format(format, buyerName, itemBase.GetLocalizedName()));

                    //[TentuPlay] 아이템 판매완료, 골드 증가
                    //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                    var sellerAgentBalance = eval.OutputStates.GetBalance(sellerAgentAddress, GoldCurrency);
                    var total = sellerAgentBalance + gold;
                    new TPStashEvent().CharacterCurrencyGet(
                        player_uuid: sellerAgentAddress.ToHex(), // seller == 본인인지 확인필요
                        character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                        currency_slug: "gold",
                        currency_quantity: float.Parse(gold.GetQuantityString()),
                        currency_total_quantity: float.Parse(total.GetQuantityString()),
                        reference_entity: entity.Trades,
                        reference_category_slug: "sell",
                        reference_slug: itemBase.Id.ToString() //아이템 품번
                    );

                    renderQuestAvatarAddress = sellerAvatarAddress;
                    renderQuestCompletedQuestIds = sellerAvatar.questList.completedQuestIds;
                }

                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                RenderQuest(renderQuestAvatarAddress, renderQuestCompletedQuestIds);
            }
        }

        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            if (eval.Exception is null)
            {
                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            UpdateCurrentAvatarState(eval);
                            UpdateWeeklyArenaState(eval);
                            var avatarState =
                                eval.OutputStates.GetAvatarState(eval.Action.avatarAddress);
                            RenderQuest(eval.Action.avatarAddress,
                                avatarState.questList.completedQuestIds);
                            _disposableForBattleEnd = null;
                        });

                var actionFailPopup = Widget.Find<ActionFailPopup>();
                actionFailPopup.CloseCallback = null;
                actionFailPopup.Close();

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<QuestPreparation>().IsActive())
                    {
                        Widget.Find<QuestPreparation>().GoToStage(eval.Action.Result);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(eval.Action.Result);
                    }
                }
                else if (Widget.Find<StageLoadingScreen>().IsActive() &&
                         Widget.Find<BattleResult>().IsActive())
                {
                    Widget.Find<BattleResult>().NextStage(eval);
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
                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            UpdateCurrentAvatarState(eval);
                            UpdateWeeklyArenaState(eval);
                            var avatarState =
                                eval.OutputStates.GetAvatarState(eval.Action.avatarAddress);
                            RenderQuest(eval.Action.avatarAddress,
                                avatarState.questList.completedQuestIds);
                            _disposableForBattleEnd = null;
                        });

                var actionFailPopup = Widget.Find<ActionFailPopup>();
                actionFailPopup.CloseCallback = null;
                actionFailPopup.Close();

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<MimisbrunnrPreparation>().IsActive())
                    {
                        Widget.Find<MimisbrunnrPreparation>().GoToStage(eval.Action.Result);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(eval.Action.Result);
                    }
                }
                else if (Widget.Find<StageLoadingScreen>().IsActive() &&
                         Widget.Find<BattleResult>().IsActive())
                {
                    Widget.Find<BattleResult>().NextMimisbrunnrStage(eval);
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
                var weeklyArenaAddress = eval.Action.WeeklyArenaAddress;
                var avatarAddress = eval.Action.AvatarAddress;

                LocalLayerModifier.RemoveWeeklyArenaInfoActivator(weeklyArenaAddress, avatarAddress);

                //[TentuPlay] RankingBattle 참가비 사용 기록 // 위의 fixme 내용과 어떻게 연결되는지?
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                Address agentAddress = States.Instance.AgentState.address;
                if (eval.OutputStates.TryGetGoldBalance(agentAddress, GoldCurrency, out var balance))
                {
                    var total = balance - new FungibleAssetValue(balance.Currency,
                        Nekoyume.GameConfig.ArenaActivationCostNCG, 0);
                    new TPStashEvent().CharacterCurrencyUse(
                        player_uuid: agentAddress.ToHex(),
                        character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                        currency_slug: "gold",
                        currency_quantity: (float)Nekoyume.GameConfig.ArenaActivationCostNCG,
                        currency_total_quantity: float.Parse(total.GetQuantityString()),
                        reference_entity: entity.Quests,
                        reference_category_slug: "arena",
                        reference_slug: "WeeklyArenaEntryFee"
                    );
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            UpdateAgentState(eval);
                            UpdateCurrentAvatarState(eval);
                            UpdateWeeklyArenaState(eval);
                            _disposableForBattleEnd = null;
                        });

                var actionFailPopup = Widget.Find<ActionFailPopup>();
                actionFailPopup.CloseCallback = null;
                actionFailPopup.Close();

                if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
                {
                    Widget.Find<RankingBoard>().GoToStage(eval);
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

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
                var result = (ItemEnhancement.ResultModel) slot.Result;
                var itemUsable = result.itemUsable;
                var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

                // NOTE: 사용한 자원에 대한 레이어 벗기기.
                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.AddItem(avatarAddress, itemUsable.ItemId, false);
                foreach (var itemId in result.materialItemIdList)
                {
                    // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                    LocalLayerModifier.AddItem(avatarAddress, itemId, false);
                }

                // NOTE: 메일 레이어 씌우기.
                LocalLayerModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                // NOTE: 워크샵 슬롯의 모든 휘발성 상태 변경자를 제거하기.
                LocalLayerModifier.ResetCombinationSlot(slot);

                // NOTE: 노티 예약 걸기.
                var format = L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE");
                UI.Notification.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId);

                //[TentuPlay] 장비강화, 골드사용
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                if (eval.OutputStates.TryGetGoldBalance(agentAddress, GoldCurrency, out var outAgentBalance))
                {
                    var total = outAgentBalance -
                                new FungibleAssetValue(outAgentBalance.Currency, result.gold, 0);
                    new TPStashEvent().CharacterCurrencyUse(
                        player_uuid: agentAddress.ToHex(),
                        character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                        currency_slug: "gold",
                        currency_quantity: (float) result.gold,
                        currency_total_quantity: float.Parse(total.GetQuantityString()),
                        reference_entity: entity.Items, //강화가 가능하므로 장비
                        reference_category_slug: "item_enhancement",
                        reference_slug: itemUsable.Id.ToString());
                }

                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                UpdateCombinationSlotState(slot);
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
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
                UpdateCurrentAvatarState(eval);
            }
        }

        public void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                LocalLayerModifier.AddReceivableQuest(avatarAddress, id);

                var currentAvatarState = States.Instance.CurrentAvatarState;
                if (currentAvatarState.address != avatarAddress)
                {
                    continue;
                }

                var quest = currentAvatarState.questList.First(q => q.Id == id);
                var rewardMap = quest.Reward.ItemMap;

                foreach (var reward in rewardMap)
                {
                    var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet
                        .First(pair => pair.Key == reward.Key);

                    LocalLayerModifier.RemoveItem(avatarAddress, materialRow.Value.ItemId, reward.Value);
                }
            }
        }

        public static void BackToMain(bool showLoadingScreen, Exception exc)
        {
            Debug.LogException(exc);
            Game.Game.instance.Agent.SendException(exc);

            if (DoNotUsePopupError(exc, out var key, out var code, out var errorMsg))
            {
                return;
            }

            Game.Event.OnRoomEnter.Invoke(showLoadingScreen);
            Game.Game.instance.Stage.OnRoomEnterEnd
                .First()
                .Subscribe(_ => PopupError(key, code, errorMsg));

            MainCanvas.instance.InitWidgetInMain();
        }

        public static void PopupError(Exception exc)
        {
            Debug.LogException(exc);
            Game.Game.instance.Agent.SendException(exc);

            if (DoNotUsePopupError(exc, out var key, out var code, out var errorMsg))
            {
                return;
            }

            PopupError(key, code, errorMsg);
        }

        private static bool DoNotUsePopupError(Exception exc, out string key, out string code, out string errorMsg)
        {
            var tuple = ErrorCode.GetErrorCode(exc);
            key = tuple.Item1;
            code = tuple.Item2;
            errorMsg = tuple.Item3;
            if (code == "27")
            {
                // NOTE: `ActionTimeoutException` 이지만 아직 해당 액션이 스테이지 되어 있을 경우(27)에는 무시합니다.
                // 이 경우 `Game.Game.Instance.Agent`에서 블록 싱크를 시도하며 결과적으로 싱크에 성공하거나 `Disconnected`가 됩니다.
                // 싱크에 성공할 경우에는 `UnableToRenderWhenSyncingBlocksException` 예외로 다시 들어옵니다.
                // `Disconnected`가 될 경우에는 이 `BackToMain`이 호출되지 않고 `Game.Game.Instance.QuitWithAgentConnectionError()`가 호출됩니다.
                return true;
            }

            return false;
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
                .Find<Alert>()
                .Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                    L10nManager.Localize("UI_OK"), false);
        }
    }
}
