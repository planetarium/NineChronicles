using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager
    {
        private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(GameConfig.WaitSeconds);

        private readonly IAgent _agent;

        private readonly ActionRenderer _renderer;

        private void ProcessAction(GameAction gameAction)
        {
            _agent.EnqueueAction(gameAction);
        }

        public ActionManager(IAgent agent)
        {
            _agent = agent;
            _renderer = agent.ActionRenderer;
        }

        #region Actions

        public IObservable<ActionBase.ActionEvaluation<CreateAvatar>> CreateAvatar(Address avatarAddress, int index,
            string nickName, int hair = 0, int lens = 0, int ear = 0, int tail = 0)
        {
            if (States.Instance.AvatarStates.ContainsKey(index))
            {
                throw new Exception($"Already contains {index} in {States.Instance.AvatarStates}");
            }

            var action = new CreateAvatar
            {
                avatarAddress = avatarAddress,
                index = index,
                hair = hair,
                lens = lens,
                ear = ear,
                tail = tail,
                name = nickName,
            };
            ProcessAction(action);

            return _renderer.EveryRender<CreateAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<DeleteAvatar>> DeleteAvatar(int index)
        {
            if (!States.Instance.AvatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"Not found {index} in {States.Instance.AvatarStates}");
            }

            var avatarAddress = States.Instance.AvatarStates[index].address;
            var action = new DeleteAvatar
            {
                index = index,
                avatarAddress = avatarAddress,
            };
            ProcessAction(action);

            return _renderer.EveryRender<DeleteAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<int> costumes,
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId)
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
            {
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // NOTE: HAS를 할 때에만 장착 여부를 저장한다.
            // 따라서 이때에 찌꺼기를 남기지 않기 위해서 장착에 대한 모든 로컬 상태를 비워준다.
            LocalStateModifier.ClearEquipOrUnequipOfCostumeAndEquipment(avatarAddress, false);

            var action = new HackAndSlash
            {
                costumes = costumes,
                equipments = equipments.Select(e => e.ItemId).ToList(),
                foods = foods.Select(f => f.ItemId).ToList(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = avatarAddress,
                WeeklyArenaAddress = weeklyArenaAddress,
            };
            ProcessAction(action);

            var itemIDs = equipments.Select(e => e.Id).Concat(foods.Select(f => f.Id)).ToArray();
            AnalyticsManager.Instance.Battle(itemIDs);
            return _renderer.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
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
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(ItemUsable itemUsable, decimal price)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalStateModifier.SetEquipmentEquip(avatarAddress, itemUsable.ItemId, false, false);

            var action = new Sell
            {
                sellerAvatarAddress = avatarAddress,
                productId = Guid.NewGuid(),
                itemId = itemUsable.ItemId,
                price = price
            };
            ProcessAction(action);

            return _renderer.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout); // Last() is for completion
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
                .Timeout(ActionTimeout); // Last() is for completion
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
                .Timeout(ActionTimeout); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<DailyReward>> DailyReward()
        {
            // NOTE: 이곳에서 하는 것이 바람직 하지만, 연출 타이밍을 위해 밖에서 한다.
            // var avatarAddress = States.Instance.CurrentAvatarState.address;
            // LocalStateModifier.ModifyAvatarDailyRewardReceivedIndex(avatarAddress, true);
            // LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, GameConfig.ActionPointMax);

            var action = new DailyReward
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                refillPoint = States.Instance.GameConfigState.ActionPointMax
            };
            ProcessAction(action);

            return _renderer.EveryRender<DailyReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<ItemEnhancement>> ItemEnhancement(Guid itemId, IEnumerable<Guid> materialIds, int slotIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // NOTE: 장착했는지 안 했는지에 상관없이 해제 플래그를 걸어 둔다.
            LocalStateModifier.SetEquipmentEquip(avatarAddress, itemId, false, false);
            foreach (var materialId in materialIds)
            {
                LocalStateModifier.SetEquipmentEquip(avatarAddress, materialId, false, false);
            }

            var action = new ItemEnhancement
            {
                itemId = itemId,
                materialIds = materialIds,
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
            };
            ProcessAction(action);

            return _renderer.EveryRender<ItemEnhancement>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<QuestReward>> QuestReward(int id)
        {
            var action = new QuestReward
            {
                questId = id,
                avatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return _renderer.EveryRender<QuestReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<RankingBattle>> RankingBattle(
            Address enemyAddress,
            List<int> costumeIds,
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
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<WeeklyArenaReward>> WeeklyArenaReward()
        {
            var action = new WeeklyArenaReward
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                WeeklyArenaAddress = ArenaHelper.GetPrevWeekAddress()
            };
            ProcessAction(action);

            return _renderer.EveryRender<WeeklyArenaReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
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
            int recipeId, int slotIndex, int? subRecipeId = null)
        {
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
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<RapidCombination>> RapidCombination(int slotIndex)
        {
            var action = new RapidCombination
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                slotIndex = slotIndex
            };
            ProcessAction(action);

            return _renderer.EveryRender<RapidCombination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<RedeemCode>> RedeemCode(PublicKey key)
        {
            var action = new RedeemCode
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                code = key
            };
            ProcessAction(action);

            return _renderer.EveryRender<RedeemCode>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
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
                .Timeout(ActionTimeout);
        }


        #endregion
    }
}
