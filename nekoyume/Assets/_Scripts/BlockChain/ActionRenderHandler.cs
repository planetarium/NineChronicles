using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet;
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
using Libplanet.Crypto;
using Nekoyume.Game;
using static Nekoyume.Model.State.RedeemCodeState;
using Nekoyume.TableData;

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

        public void Start(ActionRenderer renderer)
        {
            _renderer = renderer;

            Shop();
            Ranking();
            RewardGold();
            CreateAvatar();
            HackAndSlash();
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
            WeeklyArena();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void Shop()
        {
            _renderer.EveryRender(ShopState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateShopState).AddTo(_disposables);
        }

        private void Ranking()
        {
            var state = new RankingState((Dictionary)Game.Game.instance.Agent.GetState(Addresses.Ranking));
            foreach (var address in state.RankingMap.Keys)
            {
                _renderer.EveryRender(address)
                    .ObserveOnMainThread()
                    .Subscribe(eval => UpdateRankingMapState(eval, address)).AddTo(_disposables);
            }
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
                            currency_quantity: float.Parse((balance - ReactiveAgentState.Gold.Value).GetQuantityString()),
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
            _renderer.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    //[TentuPlay] 캐릭터 획득
                    Address agentAddress = States.Instance.AgentState.address;
                    new TPStashEvent().PlayerCharacterGet(
                            player_uuid: agentAddress.ToHex(),
                            character_uuid: eval.Action.avatarAddress.ToHex().Substring(0, 4),
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

        private void CombinationConsumable()
        {
            _renderer.EveryRender<CombinationConsumable>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationConsumable).AddTo(_disposables);
        }

        private void Sell()
        {
            _renderer.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
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
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement).AddTo(_disposables);
        }

        private void DailyReward()
        {
            _renderer.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var avatarAddress = eval.Action.avatarAddress;
                    LocalStateModifier.ModifyAvatarDailyRewardReceivedIndex(avatarAddress, false);
                    LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -States.Instance.GameConfigState.ActionPointMax);
                    UpdateCurrentAvatarState(eval);
                }).AddTo(_disposables);
        }

        private void RankingBattle()
        {
            _renderer.EveryRender<RankingBattle>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseRankingBattle).AddTo(_disposables);
        }

        private void CombinationEquipment()
        {
            _renderer.EveryRender<CombinationEquipment>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationEquipment).AddTo(_disposables);
        }

        private void RapidCombination()
        {
            _renderer.EveryRender<RapidCombination>()
                .Where(ValidateEvaluationForCurrentAvatarState)
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
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseChargeActionPoint).AddTo(_disposables);
        }
        private void WeeklyArena()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            if (ArenaHelper.TryGetThisWeekAddress(blockIndex, out var thisWeekAddress))
            {
                _renderer.EveryRender(thisWeekAddress)
                    .ObserveOnMainThread()
                    .Subscribe(UpdateWeeklyArenaState).AddTo(_disposables);

                var nextWeekAddress = ArenaHelper.GetNextWeekAddress(blockIndex);
                _renderer.EveryRender(nextWeekAddress)
                    .ObserveOnMainThread()
                    .Subscribe(UpdateWeeklyArenaState).AddTo(_disposables);
            }
        }

        private void ResponseRapidCombination(ActionBase.ActionEvaluation<RapidCombination> eval)
        {
            var avatarAddress = eval.Action.avatarAddress;
            var slot =
                eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (RapidCombination.ResultModel) slot.Result;
            foreach (var pair in result.cost)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }
            LocalStateModifier.RemoveAvatarItemRequiredIndex(avatarAddress, result.itemUsable.ItemId);

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
            UpdateCombinationSlotState(slot, eval.Action.slotIndex);
        }

        private void ResponseCombinationEquipment(ActionBase.ActionEvaluation<CombinationEquipment> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.AvatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.SlotIndex);
            var result = (CombinationConsumable.ResultModel) slot.Result;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            // NOTE: 사용한 자원에 대한 레이어 벗기기.
            LocalStateModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            foreach (var pair in result.materials)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }

            // NOTE: 메일 레이어 씌우기.
            LocalStateModifier.RemoveItem(avatarAddress, result.itemUsable.ItemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);

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
            UpdateCombinationSlotState(slot, eval.Action.SlotIndex);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

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

        private void ResponseCombinationConsumable(ActionBase.ActionEvaluation<CombinationConsumable> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.AvatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (CombinationConsumable.ResultModel) slot.Result;
            var itemUsable = result.itemUsable;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            LocalStateModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            foreach (var pair in result.materials)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }

            LocalStateModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);

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
            UpdateCombinationSlotState(slot, eval.Action.slotIndex);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.sellerAvatarAddress;
                var itemId = eval.Action.itemId;

                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, itemId, false);
                var format = L10nManager.Localize("NOTIFICATION_SELL_COMPLETE");
                var shopState = new ShopState((Dictionary) eval.OutputStates.GetState(ShopState.Address));
                var shopItem = shopState.Products.Values.First(r => r.ItemUsable.ItemId == itemId);
                UI.Notification.Push(MailType.Auction, string.Format(format, shopItem.ItemUsable.GetLocalizedName()));
                UpdateCurrentAvatarState(eval);
            }
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            var avatarAddress = eval.Action.sellerAvatarAddress;
            var result = eval.Action.result;
            var itemId = result.itemUsable.ItemId;

            LocalStateModifier.RemoveItem(avatarAddress, itemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);
            var format = L10nManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.result.itemUsable.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
            var price = eval.Action.sellerResult.shopItem.Price;
            Address renderQuestAvatarAddress;
            List<int> renderQuestCompletedQuestIds = null;

            if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                var buyerAgentAddress = States.Instance.AgentState.address;
                var result = eval.Action.buyerResult;
                var itemId = result.itemUsable.ItemId;
                var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                LocalStateModifier.ModifyAgentGold(buyerAgentAddress, price);
                LocalStateModifier.RemoveItem(buyerAvatarAddress, itemId);
                LocalStateModifier.AddNewAttachmentMail(buyerAvatarAddress, result.id);

                var format = L10nManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.buyerResult.itemUsable.GetLocalizedName()));

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
                        reference_slug: result.itemUsable.Id.ToString() //아이템 품번
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
                var itemId = result.itemUsable.ItemId;
                var gold = result.gold;
                var sellerAvatar = eval.OutputStates.GetAvatarState(sellerAvatarAddress);

                LocalStateModifier.ModifyAgentGold(sellerAgentAddress, -gold);
                LocalStateModifier.AddNewAttachmentMail(sellerAvatarAddress, result.id);

                var format = L10nManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE");
                var buyerName =
                    new AvatarState(
                            (Bencodex.Types.Dictionary) eval.OutputStates.GetState(eval.Action.buyerAvatarAddress))
                        .NameWithHash;
                UI.Notification.Push(MailType.Auction, string.Format(format, buyerName, result.itemUsable.GetLocalizedName()));

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
                    reference_slug: result.itemUsable.Id.ToString() //아이템 품번
                );

                renderQuestAvatarAddress = sellerAvatarAddress;
                renderQuestCompletedQuestIds = sellerAvatar.questList.completedQuestIds;
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            RenderQuest(renderQuestAvatarAddress, renderQuestCompletedQuestIds);
        }

        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            if (eval.Exception is null)
            {
                Game.Game.instance.Stage.onEnterToStageEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        UpdateCurrentAvatarState(eval);
                        UpdateWeeklyArenaState(eval);
                        var avatarState =
                            eval.OutputStates.GetAvatarState(eval.Action.avatarAddress);
                        States.Instance.SetCombinationSlotStates(avatarState);
                        RenderQuest(eval.Action.avatarAddress,
                            avatarState.questList.completedQuestIds);
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

                Event.OnRoomEnter.Invoke(showLoadingScreen);
                Game.Game.instance.Stage.OnRoomEnterEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        var exc = eval.Exception.InnerException;
                        var key = "ERROR_UNKNOWN";
                        switch (exc)
                        {
                            case RequiredBlockIndexException _:
                                key = "ERROR_REQUIRE_BLOCK";
                                break;
                            case EquipmentSlotUnlockException _:
                                key = "ERROR_SLOT_UNLOCK";
                                break;
                            case NotEnoughActionPointException _:
                                key = "ERROR_ACTION_POINT";
                                break;
                        }
                        var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                            L10nManager.Localize(key));
                        Widget
                            .Find<Alert>()
                            .Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                                L10nManager.Localize("UI_OK"), false);
                    });
            }
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (ItemEnhancement.ResultModel) slot.Result;
            var itemUsable = result.itemUsable;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            // NOTE: 사용한 자원에 대한 레이어 벗기기.
            LocalStateModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            LocalStateModifier.AddItem(avatarAddress, itemUsable.ItemId, false);
            foreach (var itemId in result.materialItemIdList)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, itemId, false);
            }

            // NOTE: 메일 레이어 씌우기.
            LocalStateModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);

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
            UpdateCombinationSlotState(slot, eval.Action.slotIndex);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseRankingBattle(ActionBase.ActionEvaluation<RankingBattle> eval)
        {
            if (eval.Exception is null)
            {
                var weeklyArenaAddress = eval.Action.WeeklyArenaAddress;
                var avatarAddress = eval.Action.AvatarAddress;

                LocalStateModifier.RemoveWeeklyArenaInfoActivator(weeklyArenaAddress, avatarAddress);

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

                UpdateAgentState(eval);
                UpdateCurrentAvatarState(eval);
                UpdateWeeklyArenaState(eval);

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

                Event.OnRoomEnter.Invoke(showLoadingScreen);
                Game.Game.instance.Stage.OnRoomEnterEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        var exc = eval.Exception.InnerException;
                        var key = "ERROR_UNKNOWN";
                        switch (exc)
                        {
                            case InvalidAddressException _:
                                key = "ERROR_INVALID_ADDRESS";
                                break;
                            case FailedLoadStateException _:
                                key = "ERROR_FAILED_LOAD_STATE";
                                break;
                            case NotEnoughClearedStageLevelException _:
                                key = "ERROR_NOT_ENOUGH_CLEARED_STAGE_LEVEL";
                                break;
                            case WeeklyArenaStateAlreadyEndedException _:
                                key = "ERROR_WEEKLY_ARENA_STATE_ALREADY_ENDED";
                                break;
                            case WeeklyArenaStateNotContainsAvatarAddressException _:
                                key = "ERROR_WEEKLY_ARENA_STATE_NOT_CONTAINS_AVATAR_ADDRESS";
                                break;
                            case NotEnoughWeeklyArenaChallengeCountException _:
                                key = "ERROR_NOT_ENOUGH_WEEKLY_ARENA_CHALLENGE_COUNT";
                                break;
                            case NotEnoughFungibleAssetValueException _:
                                key = "ERROR_NOT_ENOUGH_FUNGIBLE_ASSET_VALUE";
                                break;
                        }
                        var errorMsg = string.Format(
                            L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                            L10nManager.Localize(key));
                        Widget.Find<Alert>().Show(
                            L10nManager.Localize("UI_ERROR"),
                            errorMsg,
                            L10nManager.Localize("UI_OK"),
                            false);
                    });
            }
        }

        private void ResponseRedeemCode(ActionBase.ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            if (eval.Exception is null)
            {
                Widget.Find<CodeReward>().Show();
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
            var avatarAddress = eval.Action.avatarAddress;
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -States.Instance.GameConfigState.ActionPointMax);
            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            LocalStateModifier.AddItem(avatarAddress, row.ItemId, 1);
            UpdateCurrentAvatarState(eval);
        }

        public void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                LocalStateModifier.AddReceivableQuest(avatarAddress, id);

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

                    LocalStateModifier.RemoveItem(avatarAddress, materialRow.Value.ItemId, reward.Value);
                }
            }
        }
    }
}
