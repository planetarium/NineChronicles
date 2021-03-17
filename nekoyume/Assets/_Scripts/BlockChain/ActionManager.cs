using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using Nekoyume.State;
using UniRx;
using mixpanel;
using Nekoyume.UI;
using RedeemCode = Nekoyume.Action.RedeemCode;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager
    {
        private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(360f);

        private readonly IAgent _agent;

        private readonly ActionRenderer _renderer;

        private void ProcessAction(GameAction gameAction)
        {
            _agent.EnqueueAction(gameAction);
        }

        private void HandleException(Guid actionId, Exception e)
        {
            if (e is TimeoutException)
            {
                throw new ActionTimeoutException(e.Message, actionId);
            }

            throw e;
        }

        public ActionManager(IAgent agent)
        {
            _agent = agent;
            _renderer = agent.ActionRenderer;
        }

        #region Actions

        public IObservable<ActionBase.ActionEvaluation<CreateAvatar2>> CreateAvatar(int index,
            string nickName, int hair = 0, int lens = 0, int ear = 0, int tail = 0)
        {
            if (States.Instance.AvatarStates.ContainsKey(index))
            {
                throw new Exception($"Already contains {index} in {States.Instance.AvatarStates}");
            }

            var action = new CreateAvatar2
            {
                index = index,
                hair = hair,
                lens = lens,
                ear = ear,
                tail = tail,
                name = nickName,
            };
            ProcessAction(action);

            return _renderer.EveryRender<CreateAvatar2>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
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
                    Dialog.DeleteDialogPlayerPrefs(avatarAddress);
                });
        }

        public IObservable<ActionBase.ActionEvaluation<MimisbrunnrBattle>> MimisbrunnrBattle(
            List<Costume> costumes,
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId)
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
            {
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            }

            Mixpanel.Track("Unity/Create Mimisbrunnr");

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes = costumes ?? new List<Costume>();
            equipments = equipments ?? new List<Equipment>();
            foods = foods ?? new List<Consumable>();

            var action = new MimisbrunnrBattle
            {
                costumes = costumes.Select(e => e.ItemId).ToList(),
                equipments = equipments.Select(e => e.ItemId).ToList(),
                foods = foods.Select(f => f.ItemId).ToList(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = avatarAddress,
                WeeklyArenaAddress = weeklyArenaAddress,
                RankingMapAddress = States.Instance.CurrentAvatarState.RankingMapAddress,
            };
            ProcessAction(action);

            var itemIDs = equipments
                .Select(e => e.Id)
                .Concat(foods.Select(f => f.Id))
                .ToArray();
            AnalyticsManager.Instance.Battle(itemIDs);
            return _renderer.EveryRender<MimisbrunnrBattle>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            Player player,
            int worldId,
            int stageId)
        {
            return HackAndSlash(
                player.Costumes,
                player.Equipments,
                null,
                worldId,
                stageId);
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Costume> costumes,
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId)
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
            {
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            }

            Mixpanel.Track("Unity/Create HackAndSlash");

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            costumes = costumes ?? new List<Costume>();
            equipments = equipments ?? new List<Equipment>();
            foods = foods ?? new List<Consumable>();

            var action = new HackAndSlash
            {
                costumes = costumes.Select(c => c.ItemId).ToList(),
                equipments = equipments.Select(e => e.ItemId).ToList(),
                foods = foods.Select(f => f.ItemId).ToList(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = avatarAddress,
                WeeklyArenaAddress = weeklyArenaAddress,
                RankingMapAddress = States.Instance.CurrentAvatarState.RankingMapAddress,
            };
            ProcessAction(action);

            var itemIDs = equipments
                .Select(e => e.Id)
                .Concat(foods.Select(f => f.Id))
                .ToArray();
            AnalyticsManager.Instance.Battle(itemIDs);
            return _renderer.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<CombinationConsumable>> CombinationConsumable(
            int recipeId, int slotIndex)
        {
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombination);

            var action = new CombinationConsumable
            {
                recipeId = recipeId,
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex,
            };
            ProcessAction(action);

            return _renderer.EveryRender<CombinationConsumable>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(INonFungibleItem item, FungibleAssetValue price)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalLayerModifier.SetItemEquip(avatarAddress, item.ItemId, false, false);

            var action = new Sell
            {
                sellerAvatarAddress = avatarAddress,
                itemId = item.ItemId,
                price = price
            };
            ProcessAction(action);

            return _renderer.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e)); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<SellCancellation>> SellCancellation(
            Address sellerAvatarAddress,
            Guid productId)
        {
            var action = new SellCancellation
            {
                productId = productId,
                sellerAvatarAddress = sellerAvatarAddress,
            };
            ProcessAction(action);

            return _renderer.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e)); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<Buy>> Buy(Address sellerAgentAddress,
            Address sellerAvatarAddress, Guid productId)
        {
            var action = new Buy
            {
                buyerAvatarAddress = States.Instance.CurrentAvatarState.address,
                sellerAgentAddress = sellerAgentAddress,
                sellerAvatarAddress = sellerAvatarAddress,
                productId = productId
            };
            ProcessAction(action);

            return _renderer.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e)); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<DailyReward>> DailyReward()
        {
            // NOTE: 이곳에서 하는 것이 바람직 하지만, 연출 타이밍을 위해 밖에서 한다.
            // var avatarAddress = States.Instance.CurrentAvatarState.address;
            // LocalLayerModifier.ModifyAvatarDailyRewardReceivedIndex(avatarAddress, true);
            // LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, GameConfig.ActionPointMax);

            var action = new DailyReward
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return _renderer.EveryRender<DailyReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<ItemEnhancement>> ItemEnhancement(
            Guid itemId,
            Guid materialId,
            int slotIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalLayerModifier.SetItemEquip(avatarAddress, itemId, false, false);
            LocalLayerModifier.SetItemEquip(avatarAddress, materialId, false, false);

            var action = new ItemEnhancement
            {
                itemId = itemId,
                materialId = materialId,
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
            };
            ProcessAction(action);

            return _renderer.EveryRender<ItemEnhancement>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<RankingBattle>> RankingBattle(
            Address enemyAddress,
            List<Guid> costumeIds,
            List<Guid> equipmentIds,
            List<Guid> consumableIds
        )
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
                throw new NullReferenceException(nameof(weeklyArenaAddress));

            var action = new RankingBattle
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                EnemyAddress = enemyAddress,
                WeeklyArenaAddress = weeklyArenaAddress,
                costumeIds = costumeIds,
                equipmentIds = equipmentIds,
                consumableIds = consumableIds
            };
            ProcessAction(action);

            return _renderer.EveryRender<RankingBattle>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public void PatchTableSheet(string tableName, string tableCsv)
        {
            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv,
            };
            ProcessAction(action);
        }

        public IObservable<ActionBase.ActionEvaluation<CombinationEquipment>> CombinationEquipment(
            int recipeId,
            int slotIndex,
            int? subRecipeId = null)
        {
            Mixpanel.Track("Unity/Create CombinationEquipment");

            // 결과 주소도 고정되게 바꿔야함
            var action = new CombinationEquipment
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                RecipeId = recipeId,
                SubRecipeId = subRecipeId,
                SlotIndex = slotIndex,
            };
            ProcessAction(action);

            return _renderer.EveryRender<CombinationEquipment>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<RapidCombination2>> RapidCombination(int slotIndex)
        {
            var action = new RapidCombination2
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex
            };
            ProcessAction(action);

            return _renderer.EveryRender<RapidCombination2>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
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

            return _renderer.EveryRender<RedeemCode>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }

        public IObservable<ActionBase.ActionEvaluation<ChargeActionPoint>> ChargeActionPoint()
        {
            var action = new ChargeActionPoint
            {
                avatarAddress = States.Instance.CurrentAvatarState.address
            };
            ProcessAction(action);

            return _renderer.EveryRender<ChargeActionPoint>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout)
                .DoOnError(e => HandleException(action.Id, e));
        }


        #endregion
    }
}
