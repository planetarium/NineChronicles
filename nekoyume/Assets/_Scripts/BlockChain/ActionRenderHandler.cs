using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Mail;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using UniRx;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Org.BouncyCastle.Math.EC;
using TentuPlay.Api;

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
            DeleteAvatar();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            RankingReward();
            DailyReward();
            ItemEnhancement();
            QuestReward();
            RankingBattle();
            WeeklyArenaReward();
            CombinationEquipment();
            RapidCombination();
            GameConfig();
            RedeemCode();
            ChargeActionPoint();
            OpenChest();
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
            _renderer.EveryRender(RankingState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateRankingState).AddTo(_disposables);
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
                    BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
                    new TPStashEvent().CurrencyGet(
                        player_uuid: agentAddress.ToHex(),
                        currency_slug: "gold",
                        currency_quantity: (float)eval.Action.Gold,
                        currency_total_quantity: (float)(outAgentBalance + eval.Action.Gold),
                        reference_entity: "bonuses",
                        reference_category_slug: "reward_gold",
                        reference_slug: "RewardGold");

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
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void DeleteAvatar()
        {
            _renderer.EveryRender<DeleteAvatar>()
                .Where(ValidateEvaluationForAgentState)
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
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlash).AddTo(_disposables);
        }

        private void Combination()
        {
            _renderer.EveryRender<CombinationConsumable>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombination).AddTo(_disposables);
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

        private void RankingReward()
        {
            _renderer.EveryRender<RankingReward>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {

                    Address[] agentAddresses = eval.Action.agentAddresses;
                    for (var index = 0; index < agentAddresses.Length; index++)
                    {
                        Address thisAddress = agentAddresses[index];

                        if(index < 3) // index 는 3보다 작아야 => 0,1,2 만가능
                        {
                            try
                            {
                                float gold = 0.0F;

                                if (index == 0)
                                {
                                    gold = (float) eval.Action.gold1;
                                }
                                else if (index == 1)
                                {
                                    gold = (float)eval.Action.gold2;
                                }
                                else
                                {
                                    gold = (float)eval.Action.gold3;
                                }

                                //[TentuPlay] RankingReward 기록
                                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                                Address agentAddress = States.Instance.AgentState.address;
                                BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
                                new TPStashEvent().CurrencyGet(
                                    player_uuid: agentAddress.ToHex(),
                                    currency_slug: "gold",
                                    currency_quantity: gold,
                                    currency_total_quantity: (float)outAgentBalance + gold,
                                    reference_entity: "quests",
                                    reference_category_slug: "arena",
                                    reference_slug: "RankingRewardIndex" + index.ToString()
                                );
                            }
                            catch
                            {
                                // TentuPlay 실행 시 혹시 에러가 나더라도 넘어가도록.
                            }
                        }
                    }

                    UpdateAgentState(eval);

                }).AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _renderer.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForAgentState)
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

        private void QuestReward()
        {
            _renderer.EveryRender<QuestReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseQuestReward).AddTo(_disposables);
        }

        private void RankingBattle()
        {
            _renderer.EveryRender<RankingBattle>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseRankingBattle).AddTo(_disposables);
        }

        private void WeeklyArenaReward()
        {
            _renderer.EveryRender<WeeklyArenaReward>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseWeeklyArenaReward).AddTo(_disposables);
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

        private void OpenChest()
        {
            _renderer.EveryRender<OpenChest>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseOpenChest).AddTo(_disposables);
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
            new TPStashEvent().CurrencyUse(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "hourglass",
                currency_quantity: (float) (prevQty - qty),
                currency_total_quantity: (float) qty,
                reference_entity: "items_consumables",
                reference_category_slug: "consumables_rapid_combination",
                reference_slug: slot.Result.itemUsable.Id.ToString());

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

            LocalStateModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            foreach (var pair in result.materials)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }
            LocalStateModifier.RemoveItem(avatarAddress, result.itemUsable.ItemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
            var quest = Game.Game.instance.TableSheets.CombinationEquipmentQuestSheet.OrderedList
                .FirstOrDefault(row =>
                    row.RecipeId == eval.Action.RecipeId &&
                    row.SubRecipeId == eval.Action.SubRecipeId);
            if (!(quest is null))
            {
                Widget.Find<QuestResult>().Show(quest);
            }

            var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            UI.Notification.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId
            );
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);

            //[TentuPlay] Equipment 합성에 사용한 골드 기록
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
            new TPStashEvent().CurrencyUse(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)result.gold,
                currency_total_quantity: (float)(outAgentBalance - result.gold),
                reference_entity: "items_equipments",
                reference_category_slug: "equipments_combination",
                reference_slug: result.itemUsable.Id.ToString());

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UpdateCombinationSlotState(slot, eval.Action.SlotIndex);
            Widget.Find<Combination>().UpdateRecipe();
        }

        private void ResponseCombination(ActionBase.ActionEvaluation<CombinationConsumable> eval)
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
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

            var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            UI.Notification.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId
            );
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);

            //[TentuPlay] Consumable 합성에 사용한 골드 기록
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
            new TPStashEvent().CurrencyUse(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)result.gold,
                currency_total_quantity: (float)(outAgentBalance - result.gold),
                reference_entity: "items_consumables",
                reference_category_slug: "consumables_combination",
                reference_slug: result.itemUsable.Id.ToString());

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UpdateCombinationSlotState(slot, eval.Action.slotIndex);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            var avatarAddress = eval.Action.sellerAvatarAddress;
            var itemId = eval.Action.itemId;

            // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
            LocalStateModifier.AddItem(avatarAddress, itemId, false);
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_COMPLETE");
            var shopState = new ShopState((Dictionary) eval.OutputStates.GetState(ShopState.Address));
            if (shopState.TryGet(eval.Signer, eval.Action.productId, out var pair))
            {
                UI.Notification.Push(MailType.Auction, string.Format(format, pair.Value.ItemUsable.GetLocalizedName()));
            }
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            var avatarAddress = eval.Action.sellerAvatarAddress;
            var result = eval.Action.result;
            var itemId = result.itemUsable.ItemId;

            LocalStateModifier.RemoveItem(avatarAddress, itemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.result.itemUsable.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            var buyerAvatarAddress = eval.Action.buyerAvatarAddress;
            var price = eval.Action.sellerResult.shopItem.Price;

            if (buyerAvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                var buyerAgentAddress = States.Instance.AgentState.address;
                var result = eval.Action.buyerResult;
                var itemId = result.itemUsable.ItemId;
                var buyerAvatar = eval.OutputStates.GetAvatarState(buyerAvatarAddress);

                LocalStateModifier.ModifyAgentGold(buyerAgentAddress, price);
                LocalStateModifier.RemoveItem(buyerAvatarAddress, itemId);
                LocalStateModifier.AddNewAttachmentMail(buyerAvatarAddress, result.id);
                RenderQuest(buyerAvatarAddress, buyerAvatar.questList.completedQuestIds);
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.buyerResult.itemUsable.GetLocalizedName()));

                //[TentuPlay] 아이템 구입, 골드 사용
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                BigInteger buyerAgentBalance = eval.OutputStates.GetBalance(buyerAgentAddress, Currencies.Gold);
                new TPStashEvent().CurrencyUse(
                    player_uuid: States.Instance.AgentState.address.ToHex(),
                    currency_slug: "gold",
                    currency_quantity: (float) price,
                    currency_total_quantity: (float) (buyerAgentBalance - price),
                    reference_entity: "trades",
                    reference_category_slug: "buy",
                    reference_slug: result.itemUsable.Id.ToString() //아이템 품번
                );
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
                RenderQuest(sellerAvatarAddress, sellerAvatar.questList.completedQuestIds);
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE");
                var buyerName =
                    new AvatarState(
                            (Bencodex.Types.Dictionary) eval.OutputStates.GetState(eval.Action.buyerAvatarAddress))
                        .NameWithHash;
                UI.Notification.Push(MailType.Auction, string.Format(format, buyerName, result.itemUsable.GetLocalizedName()));

                //[TentuPlay] 아이템 판매완료, 골드 증가
                //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
                BigInteger sellerAgentBalance = eval.OutputStates.GetBalance(sellerAgentAddress, Currencies.Gold);
                new TPStashEvent().CurrencyGet(
                    player_uuid: sellerAgentAddress.ToHex(), // seller == 본인인지 확인필요
                    currency_slug: "gold",
                    currency_quantity: (float)gold,
                    currency_total_quantity: (float)(sellerAgentBalance + gold),
                    reference_entity: "trades",
                    reference_category_slug: "sell",
                    reference_slug: result.itemUsable.Id.ToString() //아이템 품번
                );
            }

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            Game.Game.instance.Stage.onEnterToStageEnd
                .First()
                .Subscribe(_ =>
                {
                    UpdateCurrentAvatarState(eval);
                    UpdateWeeklyArenaState(eval);
                    var avatarState = eval.OutputStates.GetAvatarState(eval.Action.avatarAddress);
                    RenderQuest(eval.Action.avatarAddress, avatarState.questList.completedQuestIds);
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

        private void ResponseQuestReward(ActionBase.ActionEvaluation<QuestReward> eval)
        {
            UpdateCurrentAvatarState(eval);
            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REWARD");
            var msg = string.Format(format, eval.Action.Result.GetContent());
            UI.Notification.Push(MailType.System, msg);
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slot = eval.OutputStates.GetCombinationSlotState(avatarAddress, eval.Action.slotIndex);
            var result = (ItemEnhancement.ResultModel) slot.Result;
            var itemUsable = result.itemUsable;
            var avatarState = eval.OutputStates.GetAvatarState(avatarAddress);

            LocalStateModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            LocalStateModifier.AddItem(avatarAddress, itemUsable.ItemId, false);
            foreach (var itemId in result.materialItemIdList)
            {
                // NOTE: 최종적으로 UpdateCurrentAvatarState()를 호출한다면, 그곳에서 상태를 새로 설정할 것이다.
                LocalStateModifier.AddItem(avatarAddress, itemId, false);
            }
            LocalStateModifier.RemoveItem(avatarAddress, itemUsable.ItemId);
            LocalStateModifier.AddNewAttachmentMail(avatarAddress, result.id);
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
            var format = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE");
            UI.Notification.Push(MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()));

            //[TentuPlay] 장비강화, 골드사용
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
            new TPStashEvent().CurrencyUse(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)result.gold,
                currency_total_quantity: (float)(outAgentBalance - result.gold),
                reference_entity: "items_equipments",     //강화가 가능하므로 장비
                reference_category_slug: "item_enhancement",
                reference_slug: itemUsable.Id.ToString());

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseRankingBattle(ActionBase.ActionEvaluation<RankingBattle> eval)
        {
            var weeklyArenaAddress = eval.Action.WeeklyArenaAddress;
            var avatarAddress = eval.Action.AvatarAddress;

            // fixme: 지금 개발 단계에서는 참가 액션이 분리되어 있지 않기 때문에, 참가할 때 깎은 골드를 더하지 못함.
            // LocalStateModifier.ModifyAgentGold(States.Instance.AgentState.address, GameConfig.ArenaActivationCostNCG);
            // fixme: 지금 개발 단계에서는 참가 액션이 분리되어 있지 않기 때문에, 참가할 때 더한 골드를 빼주지 못함.
            // LocalStateModifier.ModifyWeeklyArenaGold(-GameConfig.ArenaActivationCostNCG);
            LocalStateModifier.RemoveWeeklyArenaInfoActivator(weeklyArenaAddress, avatarAddress);

            //[TentuPlay] RankingBattle 참가비 사용 기록 // 위의 fixme 내용과 어떻게 연결되는지?
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            Address agentAddress = States.Instance.AgentState.address;
            BigInteger outAgentBalance = eval.OutputStates.GetBalance(agentAddress, Currencies.Gold);
            new TPStashEvent().CurrencyUse(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)Nekoyume.GameConfig.ArenaActivationCostNCG,
                currency_total_quantity: (float) (outAgentBalance - Nekoyume.GameConfig.ArenaActivationCostNCG),
                reference_entity: "quests",
                reference_category_slug: "arena",
                reference_slug: "WeeklyArenaEntryFee"
            );

            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
            UpdateWeeklyArenaState(eval);

            var actionFailPopup = Widget.Find<ActionFailPopup>();
            actionFailPopup.CloseCallback = null;
            actionFailPopup.Close();

            Widget.Find<RankingBoard>().GoToStage(eval);
        }

        private void ResponseWeeklyArenaReward(ActionBase.ActionEvaluation<WeeklyArenaReward> eval)
        {
            var currentBalanceState = States.Instance.GoldBalanceState;
            var balanceState = eval.OutputStates.GetGoldBalanceState(States.Instance.AgentState.address);
            var gold = balanceState.gold - currentBalanceState.gold;


            //[TentuPlay] ArenaReward 기록
            //Local에서 변경하는 States.Instance 보다는 블락에서 꺼내온 eval.OutputStates를 사용
            Address agentAddress = States.Instance.AgentState.address;

            new TPStashEvent().CurrencyGet(
                player_uuid: agentAddress.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)gold,
                currency_total_quantity: (float) (balanceState.gold + gold),
                reference_entity: "quests",
                reference_category_slug: "arena",
                reference_slug: "WeeklyArenaReward");

            UpdateAgentState(eval);
            Widget.Find<LoadingScreen>().Close();
            UI.Notification.Push(MailType.System, $"Get Arena Reward: {gold}");
        }

        private void ResponseRedeemCode(ActionBase.ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            if (eval.Exception is null)
            {
                var code = eval.Action.code;
                RedeemCodeState redeemCodeState = null;
                if (Game.Game.instance.Agent.GetState(RedeemCodeState.Address) is Dictionary d)
                {
                    redeemCodeState = new RedeemCodeState(d);
                }

                if (redeemCodeState is null)
                {
                    return;
                }

                if (redeemCodeState.Map.TryGetValue(code, out var reward))
                {
                    var tableSheets = Game.Game.instance.TableSheets;
                    var row = tableSheets.RedeemRewardSheet.Values.First(r => r.Id == reward.RewardId);
                    var rewards = row.Rewards;
                    var chestRow = tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
                    var chest = ItemFactory.CreateChest(chestRow, rewards);
                    Widget.Find<RedeemRewardPopup>().Pop(new List<(ItemBase, int)> { (chest, 1) }, tableSheets);
                    key = "UI_REDEEM_CODE_SUCCESS";
                    UpdateCurrentAvatarState(eval);
                }
            }
            else
            {
                if (eval.Exception.InnerException is DuplicateRedeemException)
                {
                    key = "UI_REDEEM_CODE_ALREADY_USE";
                }
            }

            var msg = LocalizationManager.Localize(key);
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

        private void ResponseOpenChest(ActionBase.ActionEvaluation<OpenChest> eval)
        {
            UpdateAgentState(eval);
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
