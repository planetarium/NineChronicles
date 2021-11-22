using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.Model.State;
using Nekoyume.UI;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;
using RedeemCode = Nekoyume.Action.RedeemCode;

namespace Nekoyume.BlockChain
{
    using UniRx;

    /// <summary>
    /// Creates an action of the game and puts it in the agent.
    /// </summary>
    public class ActionManager
    {
        private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(360f);

        private readonly IAgent _agent;

        private Guid _lastBattleActionId;

        public static ActionManager Instance => Game.Game.instance.ActionManager;

        public static bool IsLastBattleActionId(Guid actionId) => actionId == Instance._lastBattleActionId;

        private static void HandleException(Guid actionId, Exception e)
        {
            if (e is TimeoutException)
            {
                throw new ActionTimeoutException(e.Message, actionId);
            }

            throw e;
        }

        public ActionManager(IAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        private void ProcessAction<T>(T gameAction) where T : GameAction
        {
            var actionType =
                (ActionTypeAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(ActionTypeAttribute));
            Debug.Log($"[{nameof(ActionManager)}] {nameof(ProcessAction)}() called. \"{actionType.TypeIdentifier}\"");

            _agent.EnqueueAction(gameAction);
        }

        #region Actions

        public IObservable<ActionBase.ActionEvaluation<CreateAvatar>> CreateAvatar(int index,
            string nickName, int hair = 0, int lens = 0, int ear = 0, int tail = 0)
        {
            if (States.Instance.AvatarStates.ContainsKey(index))
            {
                throw new Exception($"Already contains {index} in {States.Instance.AvatarStates}");
            }

            var action = new CreateAvatar
            {
                index = index,
                hair = hair,
                lens = lens,
                ear = ear,
                tail = tail,
                name = nickName,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CreateAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() =>
                {
                    var agentAddress = States.Instance.AgentState.address;
                    var avatarAddress = agentAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CreateAvatar2.DeriveFormat,
                            index
                        )
                    );
                    DialogPopup.DeleteDialogPlayerPrefs(avatarAddress);
                });
        }

        public IObservable<ActionBase.ActionEvaluation<MimisbrunnrBattle>> MimisbrunnrBattle(
            List<Costume> costumes,
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId,
            int playCount)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes ??= new List<Costume>();
            equipments ??= new List<Equipment>();
            foods ??= new List<Consumable>();

            var action = new MimisbrunnrBattle
            {
                costumes = costumes.Select(e => e.ItemId).ToList(),
                equipments = equipments.Select(e => e.ItemId).ToList(),
                foods = foods.Select(f => f.ItemId).ToList(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = avatarAddress,
                rankingMapAddress = States.Instance.CurrentAvatarState.RankingMapAddress,
                playCount = playCount,
            };
            ProcessAction(action);

            _lastBattleActionId = action.Id;

            return _agent.ActionRenderer.EveryRender<MimisbrunnrBattle>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e =>
                {
                    try
                    {
                        HandleException(action.Id, e);
                    }
                    catch (Exception e2)
                    {
                        Game.Game.BackToMain(false, e2);
                    }
                });
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(Player player, int worldId, int stageId, int playCount) => HackAndSlash(
            player.Costumes,
            player.Equipments,
            null,
            worldId,
            stageId,
            playCount);

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Costume> costumes,
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId,
            int playCount)
        {
            Analyzer.Instance.Track("Unity/HackAndSlash", new Value
            {
                ["WorldId"] = worldId,
                ["StageId"] = stageId,
                ["PlayCount"] = playCount,
            });

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes ??= new List<Costume>();
            equipments ??= new List<Equipment>();
            foods ??= new List<Consumable>();

            var action = new HackAndSlash
            {
                costumes = costumes.Select(c => c.ItemId).ToList(),
                equipments = equipments.Select(e => e.ItemId).ToList(),
                foods = foods.Select(f => f.ItemId).ToList(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = avatarAddress,
                rankingMapAddress = States.Instance.CurrentAvatarState.RankingMapAddress,
            };
            ProcessAction(action);

            _lastBattleActionId = action.Id;

            return _agent.ActionRenderer.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e =>
                {
                    try
                    {
                        HandleException(action.Id, e);
                    }
                    catch (Exception e2)
                    {
                        Game.Game.BackToMain(false, e2);
                    }
                });
        }

        public IObservable<ActionBase.ActionEvaluation<CombinationConsumable>> CombinationConsumable(
            SubRecipeView.RecipeInfo recipeInfo,
            int slotIndex)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(agentAddress, -recipeInfo.CostAP);

            foreach (var (material, count) in recipeInfo.Materials)
            {
                LocalLayerModifier.RemoveItem(avatarAddress, material, count);
            }

            Analyzer.Instance.Track("Unity/Create CombinationConsumable", new Value
            {
                ["RecipeId"] = recipeInfo.RecipeId,
            });

            var action = new CombinationConsumable
            {
                recipeId = recipeInfo.RecipeId,
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CombinationConsumable>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(
            ITradableItem tradableItem,
            int count,
            FungibleAssetValue price,
            ItemSubType itemSubType)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            if (!(tradableItem is TradableMaterial))
            {
                LocalLayerModifier.RemoveItem(avatarAddress, tradableItem.TradableId, tradableItem.RequiredBlockIndex,
                    count);
            }

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalLayerModifier.SetItemEquip(avatarAddress, tradableItem.TradableId, false);

            var action = new Sell
            {
                sellerAvatarAddress = avatarAddress,
                tradableId = tradableItem.TradableId,
                count = count,
                price = price,
                itemSubType = itemSubType,
                orderId = Guid.NewGuid(),
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<SellCancellation>> SellCancellation(
            Address sellerAvatarAddress,
            Guid orderId,
            Guid tradableId,
            ItemSubType itemSubType)
        {
            var action = new SellCancellation
            {
                orderId = orderId,
                tradableId = tradableId,
                sellerAvatarAddress = sellerAvatarAddress,
                itemSubType = itemSubType,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<UpdateSell>> UpdateSell(
            Guid orderId,
            ITradableItem tradableItem,
            int count,
            FungibleAssetValue price,
            ItemSubType itemSubType)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            if (!(tradableItem is TradableMaterial))
            {
                LocalLayerModifier.RemoveItem(avatarAddress, tradableItem.TradableId, tradableItem.RequiredBlockIndex,
                    count);
            }

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalLayerModifier.SetItemEquip(avatarAddress, tradableItem.TradableId, false);

            var action = new UpdateSell
            {
                orderId = orderId,
                updateSellOrderId = Guid.NewGuid(),
                tradableId = tradableItem.TradableId,
                sellerAvatarAddress = avatarAddress,
                itemSubType = itemSubType,
                price = price,
                count = count,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<UpdateSell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<Buy>> Buy(List<PurchaseInfo> purchaseInfos)
        {
            var buyerAgentAddress = States.Instance.AgentState.address;
            foreach (var purchaseInfo in purchaseInfos)
            {
                LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -purchaseInfo.Price);
            }

            var action = new Buy
            {
                buyerAvatarAddress = States.Instance.CurrentAvatarState.address,
                purchaseInfos = purchaseInfos
            };

            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<DailyReward>> DailyReward()
        {
            var blockCount = Game.Game.instance.Agent.BlockIndex -
                States.Instance.CurrentAvatarState.dailyRewardReceivedIndex + 1;
            LocalLayerModifier.IncreaseAvatarDailyRewardReceivedIndex(
                States.Instance.CurrentAvatarState.address,
                blockCount);

            var action = new DailyReward
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<DailyReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<ItemEnhancement>> ItemEnhancement(
            Equipment baseEquipment,
            Equipment materialEquipment,
            int slotIndex,
            BigInteger costNCG)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -costNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -GameConfig.EnhanceEquipmentCostAP);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -GameConfig.EnhanceEquipmentCostAP);
            LocalLayerModifier.RemoveItem(avatarAddress, baseEquipment.TradableId,
                baseEquipment.RequiredBlockIndex, 1);
            LocalLayerModifier.RemoveItem(avatarAddress, materialEquipment.TradableId,
                materialEquipment.RequiredBlockIndex, 1);
            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalLayerModifier.SetItemEquip(avatarAddress, baseEquipment.NonFungibleId, false);
            LocalLayerModifier.SetItemEquip(avatarAddress, materialEquipment.NonFungibleId, false);

            Analyzer.Instance.Track("Unity/Item Enhancement");

            var action = new ItemEnhancement
            {
                itemId = baseEquipment.NonFungibleId,
                materialId = materialEquipment.NonFungibleId,
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<ItemEnhancement>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e =>
                {
                    try
                    {
                        HandleException(action.Id, e);
                    }
                    catch (Exception inner)
                    {
                        Game.Game.BackToMain(false, inner);
                    }
                });
        }

        public IObservable<ActionBase.ActionEvaluation<RankingBattle>> RankingBattle(
            Address enemyAddress,
            List<Guid> costumeIds,
            List<Guid> equipmentIds,
            List<Guid> consumableIds
        )
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
            {
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            }

            Analyzer.Instance.Track("Unity/Ranking Battle");
            var action = new RankingBattle
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                enemyAddress = enemyAddress,
                weeklyArenaAddress = weeklyArenaAddress,
                costumeIds = costumeIds,
                equipmentIds = equipmentIds,
                consumableIds = consumableIds
            };
            ProcessAction(action);

            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<RankingBattle>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e =>
                {
                    try
                    {
                        HandleException(action.Id, e);
                    }
                    catch (Exception e2)
                    {
                        Game.Game.BackToMain(false, e2);
                    }
                });
        }

        public IObservable<ActionBase.ActionEvaluation<PatchTableSheet>> PatchTableSheet(
            string tableName,
            string tableCsv)
        {
            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv,
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<PatchTableSheet>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<CombinationEquipment>> CombinationEquipment(
            SubRecipeView.RecipeInfo recipeInfo,
            int slotIndex)
        {
            Analyzer.Instance.Track("Unity/Create CombinationEquipment", new Value
            {
                ["RecipeId"] = recipeInfo.RecipeId,
            });

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(agentAddress, -recipeInfo.CostAP);

            foreach (var (material, count) in recipeInfo.Materials)
            {
                LocalLayerModifier.RemoveItem(avatarAddress, material, count);
            }

            var action = new CombinationEquipment
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex,
                recipeId = recipeInfo.RecipeId,
                subRecipeId = recipeInfo.SubRecipeId,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CombinationEquipment>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<RapidCombination>> RapidCombination(
            CombinationSlotState state,
            int slotIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var diff = state.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);

            LocalLayerModifier.RemoveItem(avatarAddress, materialRow.ItemId, cost);

            var action = new RapidCombination
            {
                avatarAddress = avatarAddress,
                slotIndex = slotIndex
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RapidCombination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<RedeemCode>> RedeemCode(string code)
        {
            var action = new RedeemCode(
                code,
                States.Instance.CurrentAvatarState.address
            );
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RedeemCode>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<ChargeActionPoint>> ChargeActionPoint(Material material)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.RemoveItem(avatarAddress, material.ItemId);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, States.Instance.GameConfigState.ActionPointMax);

            var action = new ChargeActionPoint
            {
                avatarAddress = avatarAddress
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<ChargeActionPoint>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        #endregion
    }
}
