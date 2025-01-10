using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.ActionExtensions;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using RedeemCode = Nekoyume.Action.RedeemCode;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Action;
#endif

namespace Nekoyume.Blockchain
{
    using Nekoyume.ApiClient;
    using UniRx;
    using static ArenaServiceClient;

    /// <summary>
    /// Creates an action of the game and puts it in the agent.
    /// </summary>
    public class ActionManager : IDisposable
    {
        private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(60f);

        private readonly IAgent _agent;

        private Guid? _lastBattleActionId;

        private readonly Dictionary<Guid, (TxId txId, long updatedBlockIndex)> _actionIdToTxIdBridge = new();

        private readonly Dictionary<Guid, DateTime> _actionEnqueuedDateTimes = new();

        private readonly List<IDisposable> _disposables = new();

        public static ActionManager Instance => Game.Game.instance.ActionManager;

        private List<Tuple<ActionBase,System.Action<TxId>>> _cachedPostProcessedActions = new();

        public static bool IsLastBattleActionId(Guid actionId)
        {
            return actionId == Instance._lastBattleActionId;
        }

        public Exception HandleException(Guid? actionId, Exception e)
        {
            if (e is TimeoutException)
            {
                var txId = actionId.HasValue
                    ? _actionIdToTxIdBridge.TryGetValue(actionId.Value, out var value)
                        ? (TxId?)value.txId
                        : null
                    : null;
                e = new ActionTimeoutException(e.Message, txId, actionId);
            }

            NcDebug.LogException(e);
            return e;
        }

        public ActionManager(IAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _agent.BlockIndexSubject.Subscribe(blockIndex =>
            {
                var actionIds = _actionIdToTxIdBridge
                    .Where(pair => pair.Value.updatedBlockIndex < blockIndex - 100)
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (var actionId in actionIds)
                {
                    _actionIdToTxIdBridge.Remove(actionId);
                }
            }).AddTo(_disposables);
            _agent.OnMakeTransaction.Subscribe(tuple =>
            {
                var (tx, actions) = tuple;
                var gameActions = actions
                    .OfType<GameAction>()
                    .ToArray();
                foreach (var gameAction in gameActions)
                {
                    _actionIdToTxIdBridge[gameAction.Id] = (tx.Id, _agent.BlockIndex);
                    var existingAction = _cachedPostProcessedActions.FirstOrDefault(a => ReferenceEquals(a.Item1, gameAction));
                    if (existingAction != null)
                    {
                        existingAction.Item2?.Invoke(tx.Id);
                        _cachedPostProcessedActions.Remove(existingAction);
                    }
                }
            }).AddTo(_disposables);
        }

        public bool TryPopActionEnqueuedDateTime(Guid actionId, out DateTime enqueuedDateTime)
        {
            if (!_actionEnqueuedDateTimes.TryGetValue(actionId, out enqueuedDateTime))
            {
                return false;
            }

            _actionEnqueuedDateTimes.Remove(actionId);
            return true;
        }

        private void ProcessAction<T>(T actionBase) where T : ActionBase
        {
            var actionType = actionBase.GetActionTypeAttribute();
            NcDebug.Log($"[{nameof(ActionManager)}] {nameof(ProcessAction)}() called. \"{actionType.TypeIdentifier}\"");

            _agent.EnqueueAction(actionBase);

            if (actionBase is GameAction gameAction)
            {
                _actionEnqueuedDateTimes[gameAction.Id] = DateTime.Now;
            }
        }

#region Actions

        public IObservable<ActionEvaluation<CreateAvatar>> CreateAvatar(
            int index,
            string nickName,
            int hair = 0,
            int lens = 0,
            int ear = 0,
            int tail = 0)
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
                name = nickName
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<CreateAvatar>()
                .Timeout(ActionTimeout)
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    Game.Game.instance.BackToNest();
                    throw HandleException(action.Id, e);
                });
        }

        public IObservable<ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Guid> costumes,
            List<Guid> equipments,
            List<Consumable> foods,
            List<RuneSlotInfo> runeInfos,
            int worldId,
            int stageId,
            int? stageBuffId = null,
            int playCount = 1,
            int apStoneCount = 0,
            bool trackGuideQuest = false)
        {
            if (trackGuideQuest)
            {
                Analyzer.Instance.Track("Unity/Click Guided Quest Enter Dungeon", new Dictionary<string, Value>()
                {
                    ["StageID"] = stageId
                });

                var evt = new AirbridgeEvent("Click_Guided_Quest_Enter_Dungeon");
                evt.SetValue(stageId);
                AirbridgeUnity.TrackEvent(evt);
            }

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/HackAndSlash",
                new Dictionary<string, Value>()
                {
                    ["WorldId"] = worldId,
                    ["StageId"] = stageId,
                    ["PlayCount"] = playCount,
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                }, true);

            var hasEvt = new AirbridgeEvent("HackAndSlash");
            hasEvt.SetValue(stageId);
            hasEvt.AddCustomAttribute("world-id", worldId);
            hasEvt.AddCustomAttribute("play-count", playCount);
            hasEvt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            hasEvt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(hasEvt);

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes ??= new List<Guid>();
            equipments ??= new List<Guid>();
            foods ??= new List<Consumable>();

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = foods.Select(f => f.ItemId).ToList(),
                RuneInfos = runeInfos,
                WorldId = worldId,
                StageId = stageId,
                StageBuffId = stageBuffId,
                AvatarAddress = avatarAddress,
                TotalPlayCount = playCount,
                ApStoneCount = apStoneCount
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<HackAndSlash>()
                .Timeout(ActionTimeout)
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    if (_lastBattleActionId == action.Id)
                    {
                        _lastBattleActionId = null;
                    }

                    Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget();
                })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<EventDungeonBattle>> EventDungeonBattle(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            List<Guid> equipments,
            List<Guid> costumes,
            List<Consumable> foods,
            List<RuneSlotInfo> runeInfos,
            bool buyTicketIfNeeded,
            bool trackGuideQuest = false)
        {
            if (trackGuideQuest)
            {
                Analyzer.Instance.Track("Unity/Click Guided Quest Enter Event Dungeon", new Dictionary<string, Value>()
                {
                    ["EventScheduleID"] = eventScheduleId,
                    ["EventDungeonID"] = eventDungeonId,
                    ["EventDungeonStageID"] = eventDungeonStageId
                });

                var evt = new AirbridgeEvent("Click_Guided_Quest_Enter_Event_Dungeon");
                evt.SetValue(eventDungeonStageId);
                evt.AddCustomAttribute("event-schedule-id", eventScheduleId);
                evt.AddCustomAttribute("event-dungeon-id", eventDungeonId);
                AirbridgeUnity.TrackEvent(evt);
            }

            var remainingTickets = RxProps.EventDungeonTicketProgress.Value.currentTickets - Action.EventDungeonBattle.PlayCount;
            var numberOfTicketPurchases = RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0;
            var ticketCostIfNeeded = buyTicketIfNeeded
                ? TableSheets.Instance.EventScheduleSheet.TryGetValue(
                    eventScheduleId,
                    out var scheduleRow)
                    ? scheduleRow.GetDungeonTicketCost(
                            numberOfTicketPurchases,
                            States.Instance.GoldBalanceState.Gold.Currency)
                        .GetQuantityString(true)
                    : "0"
                : "0";

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/EventDungeonBattle",
                new Dictionary<string, Value>()
                {
                    ["EventScheduleId"] = eventScheduleId,
                    ["EventDungeonId"] = eventDungeonId,
                    ["EventDungeonStageId"] = eventDungeonStageId,
                    ["RemainingTickets"] = remainingTickets,
                    ["NumberOfTicketPurchases"] = numberOfTicketPurchases,
                    ["BuyTicketIfNeeded"] = buyTicketIfNeeded,
                    ["TicketCostIfNeeded"] = ticketCostIfNeeded
                }, true);

            var edbEvt = new AirbridgeEvent("EventDungeonBattle");
            edbEvt.SetValue(eventDungeonStageId);
            edbEvt.AddCustomAttribute("event-schedule-id", eventScheduleId);
            edbEvt.AddCustomAttribute("event-dungeon-id", eventDungeonId);
            edbEvt.AddCustomAttribute("remaining-tickets", remainingTickets);
            edbEvt.AddCustomAttribute("number-of-ticket-purchases", numberOfTicketPurchases);
            edbEvt.AddCustomAttribute("buy-ticket-if-needed", buyTicketIfNeeded);
            edbEvt.AddCustomAttribute("ticket-cost-if-needed", ticketCostIfNeeded);
            AirbridgeUnity.TrackEvent(edbEvt);

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes ??= new List<Guid>();
            equipments ??= new List<Guid>();
            foods ??= new List<Consumable>();

            var action = new EventDungeonBattle
            {
                AvatarAddress = avatarAddress,
                EventScheduleId = eventScheduleId,
                EventDungeonId = eventDungeonId,
                EventDungeonStageId = eventDungeonStageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = foods.Select(f => f.ItemId).ToList(),
                BuyTicketIfNeeded = buyTicketIfNeeded,
                RuneInfos = runeInfos
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<EventDungeonBattle>()
                .Timeout(ActionTimeout)
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    if (_lastBattleActionId == action.Id)
                    {
                        _lastBattleActionId = null;
                    }

                    Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget();
                })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<CombinationConsumable>> CombinationConsumable(
            SubRecipeView.RecipeInfo recipeInfo,
            int slotIndex)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);

            foreach (var pair in recipeInfo.Materials)
            {
                var id = pair.Key;
                var count = pair.Value;

                if (!Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(id, out var row))
                {
                    continue;
                }

                if (recipeInfo.ReplacedMaterials.ContainsKey(row.Id))
                {
                    count = avatarState.inventory.TryGetFungibleItems(row.ItemId, out var items)
                        ? items.Sum(x => x.count)
                        : 0;
                }

                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, count);
            }

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/Create CombinationConsumable",
                new Dictionary<string, Value>()
                {
                    ["RecipeId"] = recipeInfo.RecipeId,
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                }, true);

            var evt = new AirbridgeEvent("CombinationConsumable");
            evt.SetValue(recipeInfo.RecipeId);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new CombinationConsumable
            {
                recipeId = recipeInfo.RecipeId,
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CombinationConsumable>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<EventConsumableItemCrafts>>
            EventConsumableItemCrafts(
                int eventScheduleId,
                SubRecipeView.RecipeInfo recipeInfo,
                int slotIndex)
        {
            var subRecipeId = recipeInfo.SubRecipeId ?? 0;
            var trackValue = new Dictionary<string, Value>()
            {
                ["EventScheduleId"] = eventScheduleId,
                ["RecipeId"] = recipeInfo.RecipeId,
                ["SubRecipeId"] = subRecipeId
            };
            var num = 1;
            foreach (var pair in recipeInfo.Materials)
            {
                trackValue.Add($"MaterialId_{num:00}", pair.Key);
                trackValue.Add($"MaterialCount_{num:00}", pair.Value);
                num++;
            }

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/EventConsumableItemCrafts",
                trackValue,
                true);

            var evt = new AirbridgeEvent("EventConsumableItemCrafts");
            evt.SetValue(recipeInfo.RecipeId);
            evt.AddCustomAttribute("event-schedule-id", eventScheduleId);
            evt.AddCustomAttribute("sub-recipe-id", subRecipeId);
            AirbridgeUnity.TrackEvent(evt);

            var agentAddress = States.Instance.AgentState.address;
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);

            foreach (var pair in recipeInfo.Materials)
            {
                var id = pair.Key;
                var count = pair.Value;

                if (!Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(id, out var row))
                {
                    continue;
                }

                if (recipeInfo.ReplacedMaterials.ContainsKey(row.Id))
                {
                    count = avatarState.inventory.TryGetFungibleItems(row.ItemId, out var items)
                        ? items.Sum(x => x.count)
                        : 0;
                }

                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, count);
            }

            var action = new EventConsumableItemCrafts
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                EventScheduleId = eventScheduleId,
                EventConsumableItemRecipeId = recipeInfo.RecipeId,
                SlotIndex = slotIndex
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<EventConsumableItemCrafts>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<EventMaterialItemCrafts>>
            EventMaterialItemCrafts(
                int eventScheduleId,
                int recipeId,
                Dictionary<int, int> materialsToUse)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            foreach (var (id, count) in materialsToUse)
            {
                if (!Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(id, out var row))
                {
                    continue;
                }

                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, count);
            }

            var action = new EventMaterialItemCrafts
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                EventScheduleId = eventScheduleId,
                EventMaterialItemRecipeId = recipeId,
                MaterialsToUse = materialsToUse
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<EventMaterialItemCrafts>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<HackAndSlashSweep>> HackAndSlashSweep(
            List<Guid> costumes,
            List<Guid> equipments,
            List<RuneSlotInfo> runeInfos,
            int apStoneCount,
            int actionPoint,
            int worldId,
            int stageId,
            int? playCount)
        {
            var sentryTrace = Analyzer.Instance.Track("Unity/HackAndSlashSweep", new Dictionary<string, Value>()
            {
                ["stageId"] = stageId,
                ["apStoneCount"] = apStoneCount,
                ["playCount"] = playCount,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("HackAndSlashSweep");
            evt.SetValue(stageId);
            evt.AddCustomAttribute("ap-stone-count", apStoneCount);
            evt.AddCustomAttribute("play_count", playCount);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var action = new HackAndSlashSweep
            {
                costumes = costumes,
                equipments = equipments,
                runeInfos = runeInfos,
                avatarAddress = avatarAddress,
                apStoneCount = apStoneCount,
                actionPoint = actionPoint,
                worldId = worldId,
                stageId = stageId
            };
            var apStoneRow = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            LocalLayerModifier.RemoveItem(avatarAddress, apStoneRow.ItemId, apStoneCount);
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<HackAndSlashSweep>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); }).Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<RegisterProduct>> RegisterProduct(
            Address avatarAddress,
            List<IRegisterInfo> registerInfos,
            bool chargeAp)
        {
            var registerInfo = registerInfos.First();
            var sentryTrace = Analyzer.Instance.Track("Unity/RegisterProduct", new Dictionary<string, Value>()
            {
                ["ProductType"] = registerInfo.Type.ToString(),
                ["Price"] = registerInfo.Price.ToString(),
                ["Count"] = registerInfos.Count,
                ["AvatarAddress"] = registerInfo.AvatarAddress.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("RegisterProduct");
            evt.SetValue((double)registerInfo.Price.RawValue);
            evt.AddCustomAttribute("count", registerInfos.Count);
            evt.AddCustomAttribute("product-type", registerInfo.Type.ToString());
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            if (chargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(avatarAddress);
            }

            GameConfigStateSubject.ActionPointState.Add(avatarAddress, true);

            var action = new RegisterProduct
            {
                AvatarAddress = avatarAddress,
                RegisterInfos = registerInfos,
                ChargeAp = chargeAp
            };

            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RegisterProduct>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<CancelProductRegistration>> CancelProductRegistration(
            Address avatarAddress,
            List<IProductInfo> productInfo,
            bool chargeAp)
        {
            var sentryTrace = Analyzer.Instance.Track("Unity/CancelProductRegistration", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = avatarAddress.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("CancelProductRegistration");
            evt.SetValue(productInfo.Count);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            if (chargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(avatarAddress);
            }

            GameConfigStateSubject.ActionPointState.Add(avatarAddress, true);

            var action = new CancelProductRegistration
            {
                AvatarAddress = avatarAddress,
                ProductInfos = productInfo,
                ChargeAp = chargeAp
            };

            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CancelProductRegistration>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<ReRegisterProduct>> ReRegisterProduct(
            Address avatarAddress,
            List<(IProductInfo, IRegisterInfo)> reRegisterInfos,
            bool chargeAp)
        {
            if (chargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(avatarAddress);
            }

            GameConfigStateSubject.ActionPointState.Add(avatarAddress, true);

            var action = new ReRegisterProduct
            {
                AvatarAddress = avatarAddress,
                ReRegisterInfos = reRegisterInfos,
                ChargeAp = chargeAp
            };

            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<ReRegisterProduct>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e))
                .Finally(() => { });
        }

        public IObservable<ActionEvaluation<BuyProduct>> BuyProduct(
            Address avatarAddress,
            List<IProductInfo> productInfos)
        {
            var buyerAgentAddress = States.Instance.AgentState.address;
            foreach (var info in productInfos)
            {
                LocalLayerModifier
                    .ModifyAgentGoldAsync(buyerAgentAddress, -info.Price)
                    .Forget();
            }

            var action = new BuyProduct
            {
                AvatarAddress = avatarAddress,
                ProductInfos = productInfos
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<BuyProduct>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e));
        }


        public IObservable<ActionEvaluation<Buy>> Buy(List<PurchaseInfo> purchaseInfos)
        {
            var buyerAgentAddress = States.Instance.AgentState.address;
            foreach (var purchaseInfo in purchaseInfos)
            {
                LocalLayerModifier
                    .ModifyAgentGoldAsync(buyerAgentAddress, -purchaseInfo.Price)
                    .Forget();
            }

            var action = new Buy
            {
                buyerAvatarAddress = States.Instance.CurrentAvatarState.address,
                purchaseInfos = purchaseInfos
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<Buy>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<DailyReward>> DailyReward()
        {
            var action = new DailyReward
            {
                avatarAddress = States.Instance.CurrentAvatarState.address
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<DailyReward>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => throw HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<ItemEnhancement>> ItemEnhancement(
            Equipment baseEquipment,
            List<Equipment> materialEquipments,
            int slotIndex,
            Dictionary<int, int> hammers,
            BigInteger costNCG)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -costNCG);

            if (baseEquipment.ItemSubType is ItemSubType.Aura or ItemSubType.Grimoire)
            {
                // Because aura is a tradable item, removal or addition in local layer will fail and exceptions will be handled.
                LocalLayerModifier.RemoveNonFungibleItem(avatarAddress, baseEquipment.ItemId);
            }
            else
            {
                LocalLayerModifier.RemoveItem(avatarAddress, baseEquipment.ItemId,
                    baseEquipment.RequiredBlockIndex, 1);
            }

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            foreach (var materialEquip in materialEquipments)
            {
                if (materialEquip.ItemSubType is ItemSubType.Aura or ItemSubType.Grimoire)
                {
                    // Because aura is a tradable item, removal or addition in local layer will fail and exceptions will be handled.
                    LocalLayerModifier.RemoveNonFungibleItem(avatarAddress, materialEquip.ItemId);
                }
                else
                {
                    LocalLayerModifier.RemoveItem(avatarAddress, materialEquip.ItemId,
                        materialEquip.RequiredBlockIndex, 1);
                }

                LocalLayerModifier.SetItemEquip(avatarAddress, materialEquip.NonFungibleId, false);
            }

            LocalLayerModifier.SetItemEquip(avatarAddress, baseEquipment.NonFungibleId, false);

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/Item Enhancement",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                }, true);

            var evt = new AirbridgeEvent("ItemEnhancement");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new ItemEnhancement
            {
                itemId = baseEquipment.NonFungibleId,
                materialIds = materialEquipments.Select((matEquipment) => matEquipment.NonFungibleId).ToList(),
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
                hammers = hammers
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<ItemEnhancement>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); }).Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<RankingBattle>> RankingBattle(
            Address enemyAddress,
            List<Guid> costumeIds,
            List<Guid> equipmentIds
        )
        {
            if (!ArenaHelperOld.TryGetThisWeekAddress(out var weeklyArenaAddress))
            {
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            }

            var sentryTrace = Analyzer.Instance.Track(
                "Unity/Ranking Battle",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                }, true);

            var action = new RankingBattle
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                enemyAddress = enemyAddress,
                weeklyArenaAddress = weeklyArenaAddress,
                costumeIds = costumeIds,
                equipmentIds = equipmentIds
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<RankingBattle>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    if (_lastBattleActionId == action.Id)
                    {
                        _lastBattleActionId = null;
                    }

                    Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget();
                })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<JoinArena>> JoinArena(
            List<Guid> costumes,
            List<Guid> equipments,
            List<RuneSlotInfo> runeInfos,
            int championshipId,
            int round
        )
        {
            var action = new JoinArena
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                costumes = costumes,
                equipments = equipments,
                runeInfos = runeInfos,
                championshipId = championshipId,
                round = round
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<JoinArena>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<Action.Arena.Battle>> BattleArena(
            Address enemyAvatarAddress,
            List<Guid> costumes,
            List<Guid> equipments,
            List<RuneSlotInfo> runeInfos,
            int championshipId,
            int round,
            BattleTokenResponse token
        )
        {
            // TODO: 아레나 서비스
            // 티켓이나 라운드 정보 조회 및 소모 기능 추가 후 적용
            try
            {
                var action = new Action.Arena.Battle
                {
                    myAvatarAddress = States.Instance.CurrentAvatarState.address,
                    enemyAvatarAddress = enemyAvatarAddress,
                    costumes = costumes,
                    equipments = equipments,
                    runeInfos = runeInfos,
                    memo = token.Token
                };

                var sentryTrace = Analyzer.Instance.Track("Unity/BattleArena",
                    new Dictionary<string, Value>()
                    {
                        ["championshipId"] = championshipId,
                        ["round"] = round,
                        ["enemyAvatarAddress"] = enemyAvatarAddress.ToString(),
                        ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                        ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                    }, true);

                var evt = new AirbridgeEvent("BattleArena");
                evt.SetValue(championshipId);
                evt.AddCustomAttribute("round", round);
                evt.AddCustomAttribute("enemy-avatar-address", enemyAvatarAddress.ToString());
                evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
                evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
                AirbridgeUnity.TrackEvent(evt);

                ProcessAction(action);
                
                _cachedPostProcessedActions.Add(Tuple.Create<ActionBase, Action<TxId>>(action, (txId) => {
                    // todo: 아레나 서비스
                    // 타입변경되면 수정해야함
                    // tx나 액션 보내는 시점에따라 추가변경필요할수있음.
                    var task = ApiClients.Instance.Arenaservicemanager.PostSeasonsBattleRequestAsync(txId.ToString(), token.BattleLogId, RxProps.CurrentArenaSeasonId, States.Instance.CurrentAvatarState.address.ToHex());
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            // 오류 처리
                            NcDebug.LogError($"[ActionManager] 아레나 서비스 요청 실패: {t.Exception?.Message}");
                        }
                    });
                }));
                
                _lastBattleActionId = action.Id;
                return _agent.ActionRenderer.EveryRender<Action.Arena.Battle>()
                    .Timeout(ActionTimeout)
                    .Where(eval => eval.Action.Id.Equals(action.Id))
                    .First()
                    .ObserveOnMainThread()
                    .DoOnError(e =>
                    {
                        if (_lastBattleActionId == action.Id)
                        {
                            _lastBattleActionId = null;
                        }

                        Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget();
                    }).Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
            }
            catch (Exception e)
            {
                Game.Game.BackToMainAsync(e).Forget();
                return null;
            }
        }

        public IObservable<ActionEvaluation<PatchTableSheet>> PatchTableSheet(
            string tableName,
            string tableCsv)
        {
            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<PatchTableSheet>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<CombinationEquipment>> CombinationEquipment(
            SubRecipeView.RecipeInfo recipeInfo,
            int slotIndex,
            bool payByCrystal,
            bool useHammerPoint,
            int? petId)
        {
            var sentryTx = Analyzer.Instance.Track(
                "Unity/Create CombinationEquipment",
                new Dictionary<string, Value>()
                {
                    ["RecipeId"] = recipeInfo.RecipeId,
                    ["PetId"] = petId ?? default,
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                }, true);

            var evt = new AirbridgeEvent("CombinationEquipment");
            evt.SetValue(recipeInfo.RecipeId);
            evt.AddCustomAttribute("pet-id", petId ?? default);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var agentAddress = States.Instance.AgentState.address;
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);
            if (useHammerPoint)
            {
                var recipeId = recipeInfo.RecipeId;
                var originHammerPointState = States.Instance.HammerPointStates[recipeId];
                States.Instance.UpdateHammerPointStates(
                    recipeId, new HammerPointState(originHammerPointState.Address, recipeId));
            }
            else
            {
                foreach (var pair in recipeInfo.Materials)
                {
                    var id = pair.Key;
                    var count = pair.Value;

                    if (!Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(id, out var row))
                    {
                        continue;
                    }

                    if (recipeInfo.ReplacedMaterials.ContainsKey(row.Id))
                    {
                        count = avatarState.inventory.TryGetFungibleItems(row.ItemId, out var items)
                            ? items.Sum(x => x.count)
                            : 0;
                    }

                    LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, count);
                }
            }

            var action = new CombinationEquipment
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex,
                recipeId = recipeInfo.RecipeId,
                subRecipeId = recipeInfo.SubRecipeId,
                payByCrystal = payByCrystal,
                useHammerPoint = useHammerPoint,
                petId = petId
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CombinationEquipment>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTx));
        }

        public IObservable<ActionEvaluation<RapidCombination>> RapidCombination(
            CombinationSlotState state,
            int slotIndex)
        {
            return RapidCombination(new List<CombinationSlotState> { state }, new List<int> { slotIndex });
        }

        public IObservable<ActionEvaluation<RapidCombination>> RapidCombination(
            List<CombinationSlotState> stateList,
            List<int> slotIndexList)
        {
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var hourglassDataRow = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Hourglass);

            var cost = CombinationSlotsPopup.GetWorkingSlotsOpenCost(stateList, currentBlockIndex);
            LocalLayerModifier.RemoveItem(avatarAddress, hourglassDataRow.ItemId, cost);
            var sentryTrace = Analyzer.Instance.Track(
                "Unity/Rapid Combination",
                new Dictionary<string, Value>()
                {
                    ["HourglassCount"] = cost,
                    ["AvatarAddress"] = avatarAddress.ToString(),
                    ["AgentAddress"] = agentAddress.ToString()
                }, true);

            var evt = new AirbridgeEvent("RapidCombination");
            evt.SetValue(cost);
            evt.AddCustomAttribute("agent-address", avatarAddress.ToString());
            evt.AddCustomAttribute("avatar-address", agentAddress.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new RapidCombination
            {
                avatarAddress = avatarAddress,
                slotIndexList = slotIndexList
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RapidCombination>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<RedeemCode>> RedeemCode(string code)
        {
            var action = new RedeemCode(
                code,
                States.Instance.CurrentAvatarState.address
            );
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RedeemCode>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<ChargeActionPoint>> ChargeActionPoint()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var row = TableSheets.Instance.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.ApStone);
            LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId);

            var headerMenu = Widget.Find<HeaderMenuStatic>();
            var apPortionUi = headerMenu.ApPotion;
            apPortionUi.SetActiveLoading(true);

            var action = new ChargeActionPoint
            {
                avatarAddress = avatarAddress
            };
            ProcessAction(action);

            var address = States.Instance.CurrentAvatarState.address;
            if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
            {
                GameConfigStateSubject.ActionPointState.Remove(address);
            }

            GameConfigStateSubject.ActionPointState.Add(address, true);

            NotificationSystem.Push(MailType.System, L10nManager.Localize("UI_CHARGE_AP"),
                NotificationCell.NotificationType.Information);

            return _agent.ActionRenderer.EveryRender<ChargeActionPoint>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<Grinding>> Grinding(
            List<Equipment> equipmentList,
            bool chargeAp,
            long gainedCrystal)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            equipmentList.ForEach(equipment =>
            {
                if (equipment.ItemSubType is ItemSubType.Aura or ItemSubType.Grimoire)
                {
                    // Because aura is a tradable item, removal or addition in local layer will fail and exceptions will be handled.
                    LocalLayerModifier.RemoveNonFungibleItem(
                        avatarAddress,
                        equipment.ItemId);
                }
                else
                {
                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        equipment.ItemId,
                        equipment.RequiredBlockIndex,
                        1);
                }
            });

            if (chargeAp)
            {
                ChargeAP(avatarAddress);
            }

            var sentryTrace = Analyzer.Instance.Track("Unity/Grinding", new Dictionary<string, Value>()
            {
                ["EquipmentCount"] = equipmentList.Count,
                ["GainedCrystal"] = gainedCrystal,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("Grinding");
            evt.SetValue(gainedCrystal);
            evt.AddCustomAttribute("equipment-count", equipmentList.Count);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new Grinding
            {
                AvatarAddress = avatarAddress,
                EquipmentIds = equipmentList.Select(i => i.ItemId).ToList(),
                ChargeAp = chargeAp
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<Grinding>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<Synthesize>> Synthesize(
            List<ItemBase> itemBaseList,
            bool chargeAp,
            Grade grade,
            ItemSubType itemSubType)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            itemBaseList.ForEach(itemBase =>
            {
                if (itemBase is Equipment equipment)
                {
                    if (equipment.ItemSubType is ItemSubType.Aura or ItemSubType.Grimoire)
                    {
                        // Because aura is a tradable item, removal or addition in local layer will fail and exceptions will be handled.
                        LocalLayerModifier.RemoveNonFungibleItem(
                            avatarAddress,
                            equipment.ItemId);
                    }
                    else
                    {
                        LocalLayerModifier.RemoveItem(
                            avatarAddress,
                            equipment.ItemId,
                            equipment.RequiredBlockIndex,
                            1);
                    }
                }
                else if (itemBase is Costume costume)
                {
                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        costume.ItemId,
                        costume.RequiredBlockIndex,
                        1);
                }
            });

            if (chargeAp)
            {
                ChargeAP(avatarAddress);
            }

            // TODO: If need sentry or airBridge trace, add it.

            var action = new Synthesize
            {
                AvatarAddress = avatarAddress,
                MaterialIds = itemBaseList.Select(i =>
                {
                    return i switch
                    {
                        Equipment equipment => equipment.ItemId,
                        Costume costume => costume.ItemId,
                        _ => throw new InvalidCastException(),
                    };
                }).ToList(),
                ChargeAp = chargeAp,
                MaterialGradeId = (int)grade,
                MaterialItemSubTypeId = (int)itemSubType,
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<Synthesize>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => {  });
        }

        public IObservable<ActionEvaluation<UnlockEquipmentRecipe>> UnlockEquipmentRecipe(
            List<int> recipeIdList,
            BigInteger openCost)
        {
            LocalLayerModifier
                .ModifyAgentCrystalAsync(
                    States.Instance.AgentState.address,
                    -openCost)
                .Forget();

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var sentryTrace = Analyzer.Instance.Track("Unity/UnlockEquipmentRecipe", new Dictionary<string, Value>()
            {
                ["BurntCrystal"] = (long)openCost,
                ["AvatarAddress"] = avatarAddress.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("UnlockEquipmentRecipe");
            evt.SetValue((double)openCost);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new UnlockEquipmentRecipe
            {
                AvatarAddress = avatarAddress,
                RecipeIds = recipeIdList
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<UnlockEquipmentRecipe>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }


        public IObservable<ActionEvaluation<UnlockWorld>> UnlockWorld(List<int> worldIdList, int cost)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var sentryTrace = Analyzer.Instance.Track("Unity/UnlockWorld", new Dictionary<string, Value>()
            {
                ["BurntCrystal"] = cost,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("UnlockWorld");
            evt.SetValue(cost);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new UnlockWorld
            {
                AvatarAddress = avatarAddress,
                WorldIds = worldIdList
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<UnlockWorld>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<HackAndSlashRandomBuff>> HackAndSlashRandomBuff(bool advanced, long burntCrystal)
        {
            var sentryTrace = Analyzer.Instance.Track("Unity/Purchase Crystal Bonus Skill", new Dictionary<string, Value>()
            {
                ["BurntCrystal"] = burntCrystal,
                ["isAdvanced"] = advanced,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString()
            }, true);

            var evt = new AirbridgeEvent("HackAndSlashRandomBuff");
            evt.SetValue(burntCrystal);
            evt.AddCustomAttribute("is-advanced", advanced);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var action = new HackAndSlashRandomBuff
            {
                AvatarAddress = avatarAddress,
                AdvancedGacha = advanced
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<HackAndSlashRandomBuff>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e))
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<Raid>> Raid(
            List<Guid> costumes,
            List<Guid> equipments,
            List<Guid> foods,
            List<RuneSlotInfo> runeInfos,
            bool payNcg)
        {
            var action = new Raid
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                CostumeIds = costumes,
                EquipmentIds = equipments,
                FoodIds = foods,
                RuneInfos = runeInfos,
                PayNcg = payNcg
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<Raid>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<ClaimRaidReward>> ClaimRaidReward()
        {
            var action = new ClaimRaidReward(States.Instance.CurrentAvatarState.address);
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<ClaimRaidReward>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<RuneEnhancement>> RuneEnhancement(
            int runeId,
            int tryCount)
        {
            var action = new RuneEnhancement
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                RuneId = runeId,
                TryCount = tryCount > 0 ? tryCount : 1
            };

            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<RuneEnhancement>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<UnlockRuneSlot>> UnlockRuneSlot(int slotIndex)
        {
            var action = new UnlockRuneSlot
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                SlotIndex = slotIndex
            };

            LoadingHelper.UnlockRuneSlot.Add(slotIndex);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<UnlockRuneSlot>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<UnlockCombinationSlot>> UnlockCombinationSlot(int slotIndex)
        {
            var action = new UnlockCombinationSlot
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                SlotIndex = slotIndex
            };

            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<UnlockCombinationSlot>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<PetEnhancement>> PetEnhancement(
            int petId,
            int targetLevel)
        {
            var sentryTrace = Analyzer.Instance.Track("Unity/PetEnhancement", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                ["TargetLevel"] = targetLevel,
                ["PetId"] = petId
            }, true);

            var evt = new AirbridgeEvent("PetEnhancement");
            evt.SetValue(targetLevel);
            evt.AddCustomAttribute("pet-id", petId);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new PetEnhancement
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                PetId = petId,
                TargetLevel = targetLevel
            };

            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<PetEnhancement>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); }).Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<ActivateCollection>> ActivateCollection(
            int collectionId,
            List<ICollectionMaterial> materials)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var materialsType = materials.First().Type;

            var sentryTrace = Analyzer.Instance.Track($"Unity/{nameof(ActivateCollection)}",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = avatarAddress.ToString(),
                    ["AgentAddress"] = agentAddress.ToString(),
                    ["CollectionId"] = collectionId,
                    ["MaterialsType"] = materialsType.ToString()
                }, true);

            var evt = new AirbridgeEvent(nameof(ActivateCollection));
            evt.SetValue(collectionId);
            evt.AddCustomAttribute("materials-type", materialsType.ToString());
            evt.AddCustomAttribute("agent-address", avatarAddress.ToString());
            evt.AddCustomAttribute("avatar-address", agentAddress.ToString());
            AirbridgeUnity.TrackEvent(evt);

            var action = new ActivateCollection
            {
                AvatarAddress = avatarAddress,
                CollectionData = new List<(int collectionId, List<ICollectionMaterial> materials)>
                {
                    (collectionId, materials)
                }
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ActivateCollection>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); }).Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<ApprovePledge>> ApprovePledge(Address patronAddress)
        {
            var action = new ApprovePledge
            {
                PatronAddress = patronAddress
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ApprovePledge>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Signer.Equals(States.Instance.AgentState.address))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                });
        }

        public IObservable<ActionEvaluation<AuraSummon>> AuraSummon(int groupId, int summonCount)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            // analytics
            var sentryTrace = Analyzer.Instance.Track(
                "Unity/AuraSummon",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = avatarAddress.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                    ["GroupId"] = groupId,
                    ["SummonCount"] = summonCount
                }, true);

            var action = new AuraSummon
            {
                AvatarAddress = avatarAddress,
                GroupId = groupId,
                SummonCount = summonCount
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<AuraSummon>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<RuneSummon>> RuneSummon(int groupId, int summonCount)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            // analytics
            var sentryTrace = Analyzer.Instance.Track(
                "Unity/RuneSummon",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = avatarAddress.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                    ["GroupId"] = groupId,
                    ["SummonCount"] = summonCount
                }, true);

            var action = new RuneSummon
            {
                AvatarAddress = avatarAddress,
                GroupId = groupId,
                SummonCount = summonCount
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<RuneSummon>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<CostumeSummon>> CostumeSummon(int groupId, int summonCount)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;

            // analytics
            var sentryTrace = Analyzer.Instance.Track(
                "Unity/AuraSummon",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = avatarAddress.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                    ["GroupId"] = groupId,
                    ["SummonCount"] = summonCount
                }, true);

            var action = new CostumeSummon
            {
                AvatarAddress = avatarAddress,
                GroupId = groupId,
                SummonCount = summonCount
            };
            ProcessAction(action);

            return _agent.ActionRenderer.EveryRender<CostumeSummon>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); })
                .Finally(() => Analyzer.Instance.FinishTrace(sentryTrace));
        }

        public IObservable<ActionEvaluation<ClaimItems>> ClaimItems(
            params (Address, IReadOnlyList<FungibleAssetValue>)[] claimData)
        {
            var action = new ClaimItems(claimData);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ClaimItems>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<TransferAsset>> TransferAsset(
            Address sender,
            Address recipient,
            FungibleAssetValue amount)
        {
            var action = new TransferAsset(sender, recipient, amount);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<TransferAsset>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                // .DoOnError(e => HandleException(action.Id, e));
                .DoOnError(e => { });
        }

        public IObservable<ActionEvaluation<TransferAssets>> TransferAssets(
            Address sender,
            params (Address, FungibleAssetValue)[] recipients)
        {
            var action = new TransferAssets(sender, recipients.ToList());
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<TransferAssets>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                // .DoOnError(e => HandleException(action.Id, e));
                .DoOnError(e => { });
        }

        public IObservable<ActionEvaluation<Stake>> Stake(
            BigInteger amount)
        {
            var action = new Stake(amount);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<Stake>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionEvaluation<ClaimStakeReward>> ClaimStakeReward(
            Address avatarAddress)
        {
            var action = new ClaimStakeReward(avatarAddress);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ClaimStakeReward>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                // .DoOnError(e => HandleException(action.Id, e));
                .DoOnError(e => { });
        }

        public IObservable<ActionEvaluation<ClaimGifts>> ClaimGifts(
            Address avatarAddress,
            int giftId)
        {
            var action = new ClaimGifts(avatarAddress, giftId);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ClaimGifts>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                // .DoOnError(e => HandleException(action.Id, e));
                .DoOnError(e => { });
        }

        public IObservable<ActionEvaluation<Wanted>> Wanted(long season, FungibleAssetValue amount)
        {
            var action = new Wanted
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                Bounty = amount,
                Season = (int)season
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<Wanted>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                    NcDebug.LogError("Wanted Error" + e.Message);
                });
        }

        public IObservable<ActionEvaluation<ExploreAdventureBoss>> ExploreAdventureBoss(List<Guid> costume, List<Guid> equipments, List<Guid> food, List<RuneSlotInfo> runeInfo, int seasonId)
        {
            var action = new ExploreAdventureBoss
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                Season = (int)seasonId,
                Costumes = costume,
                Equipments = equipments,
                Foods = food,
                RuneInfos = runeInfo
            };
            _lastBattleActionId = action.Id;
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ExploreAdventureBoss>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                });
        }

        public IObservable<ActionEvaluation<SweepAdventureBoss>> SweepAdventureBoss(List<Guid> costume, List<Guid> equipments, List<RuneSlotInfo> runeInfo, int seasonId)
        {
            var action = new SweepAdventureBoss
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                Season = seasonId,
                Costumes = costume,
                Equipments = equipments,
                RuneInfos = runeInfo
            };
            _lastBattleActionId = action.Id;
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<SweepAdventureBoss>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                });
        }

        public IObservable<ActionEvaluation<UnlockFloor>> UnlockFloor(bool useNCG, int seasonId)
        {
            var action = new UnlockFloor
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                Season = seasonId,
                UseNcg = useNCG
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<UnlockFloor>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                });
        }

        public IObservable<ActionEvaluation<ClaimAdventureBossReward>> ClaimAdventureBossReward(long SeasonId)
        {
            var action = new ClaimAdventureBossReward
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ClaimAdventureBossReward>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    // NOTE: Handle exception outside of this method.
                });
        }

        public IObservable<ActionEvaluation<CustomEquipmentCraft>> CustomEquipmentCraft(int slotIndex, int recipeId, int iconId)
        {
            var action = new CustomEquipmentCraft()
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                CraftList = new List<CustomCraftData>
                {
                    new()
                    {
                        IconId = iconId,
                        RecipeId = recipeId,
                        SlotIndex = slotIndex
                    }
                }
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<CustomEquipmentCraft>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(null, e));
        }

        public IObservable<ActionEvaluation<ClaimPatrolReward>> ClaimPatrolReward()
        {
            var action = new ClaimPatrolReward(States.Instance.CurrentAvatarState.address);
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ClaimPatrolReward>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.PlainValue.Equals(action.PlainValue))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => HandleException(null, e));
        }

#if UNITY_EDITOR || LIB9C_DEV_EXTENSIONS
        public IObservable<ActionEvaluation<CreateTestbed>> CreateTestbed()
        {
            var action = new CreateTestbed
            {
                weeklyArenaAddress = WeeklyArenaState.DeriveAddress(
                    (int)Game.Game.instance.Agent.BlockIndex / States.Instance.GameConfigState.WeeklyArenaInterval)
            };
            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<CreateTestbed>()
                .Timeout(ActionTimeout)
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }

        public IObservable<ActionEvaluation<CreateArenaDummy>> CreateArenaDummy(
            List<Guid> costumes,
            List<Guid> equipments,
            int championshipId,
            int round,
            int accountCount
        )
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var action = new CreateArenaDummy
            {
                myAvatarAddress = avatarAddress,
                costumes = costumes,
                equipments = equipments,
                championshipId = championshipId,
                round = round,
                accountCount = accountCount
            };
            ProcessAction(action);
            _lastBattleActionId = action.Id;
            return _agent.ActionRenderer.EveryRender<CreateArenaDummy>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e =>
                {
                    try
                    {
                        HandleException(action.Id, e);
                    }
                    catch (Exception e2)
                    {
                    }
                });
        }

        public IObservable<ActionEvaluation<ManipulateState>> ManipulateState(
            List<(Address, Address, IValue)> stateList,
            List<(Address, FungibleAssetValue)> balanceList)
        {
            var action = new ManipulateState
            {
                StateList = stateList ?? new List<(Address accountAddr, Address addr, IValue value)>(),
                BalanceList = balanceList ?? new List<(Address addr, FungibleAssetValue fav)>()
            };

            ProcessAction(action);
            return _agent.ActionRenderer.EveryRender<ManipulateState>()
                .Timeout(ActionTimeout)
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .First()
                .ObserveOnMainThread()
                .DoOnError(e => { Game.Game.BackToMainAsync(HandleException(action.Id, e)).Forget(); });
        }
#endif

#endregion

        private void ChargeAP(Address avatarAddress)
        {
            var row = TableSheets.Instance.MaterialItemSheet
                                 .OrderedList
                                 .First(r => r.ItemSubType == ItemSubType.ApStone);
            LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId);

            if (GameConfigStateSubject.ActionPointState.ContainsKey(avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(avatarAddress);
            }

            GameConfigStateSubject.ActionPointState.Add(avatarAddress, true);
        }

        public void Dispose()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
