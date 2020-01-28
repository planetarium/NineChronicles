using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Manager;
using Nekoyume.Pattern;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager : MonoSingleton<ActionManager>
    {
        private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(GameConfig.WaitSeconds);
        
        private static void ProcessAction(GameAction gameAction)
        {
            Game.Game.instance.Agent.EnqueueAction(gameAction);
        }

        #region Actions

        public IObservable<ActionBase.ActionEvaluation<CreateAvatar>> CreateAvatar(Address avatarAddress, int index,
            string nickName, int hair = 0, int lens = 0, int ear = 0, int tail = 0)
        {
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

            return ActionBase.EveryRender<CreateAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<DeleteAvatar>> DeleteAvatar(int index)
        {
            var avatarAddress = States.Instance.AvatarStates[index].address;
            var action = new DeleteAvatar
            {
                index = index,
                avatarAddress = avatarAddress,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<DeleteAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Equipment> equipments,
            List<Consumable> foods,
            int worldId,
            int stageId)
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            
            var action = new HackAndSlash
            {
                equipments = equipments,
                foods = foods,
                worldId = worldId,
                stageId = stageId,
                avatarAddress = States.Instance.CurrentAvatarState.address,
                WeeklyArenaAddress = weeklyArenaAddress,
            };
            ProcessAction(action);

            var itemIDs = equipments.Select(e => e.Data.Id).Concat(foods.Select(f => f.Data.Id)).ToArray();
            AnalyticsManager.Instance.Battle(itemIDs);
            return ActionBase.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<Combination>> Combination(
            List<(Material material, int count)> materialInfoList)
        {
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombination);

            var action = new Combination();
            materialInfoList.ForEach(info =>
            {
                var (material, count) = info;
                if (action.Materials.ContainsKey(material))
                {
                    action.Materials[material] += count;
                }
                else
                {
                    action.Materials.Add(material, count);
                }
            });
            action.AvatarAddress = States.Instance.CurrentAvatarState.address;
            ProcessAction(action);

            return ActionBase.EveryRender<Combination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(ItemUsable itemUsable, decimal price)
        {
            var action = new Sell
            {
                sellerAvatarAddress = States.Instance.CurrentAvatarState.address,
                productId = Guid.NewGuid(),
                itemUsable = itemUsable,
                price = price
            };
            ProcessAction(action);

            return ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<SellCancellation>> SellCancellation(Address sellerAvatarAddress,
            Guid productId)
        {
            var action = new SellCancellation
            {
                productId = productId,
                sellerAvatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<SellCancellation>()
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

            return ActionBase.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<AddItem>> AddItem(Guid itemId, bool canceled)
        {
            var action = new AddItem
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                itemId = itemId,
                canceled = canceled
            };
            ProcessAction(action);

            return ActionBase.EveryRender<AddItem>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<AddGold>> AddGold()
        {
            var action = new AddGold
            {
                agentAddress = States.Instance.AgentState.address,
                avatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<AddGold>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout); // Last() is for completion

        }

        public IObservable<ActionBase.ActionEvaluation<DailyReward>> DailyReward()
        {
            var action = new DailyReward
            {
                avatarAddress = States.Instance.CurrentAvatarState.address,
                refillPoint = GameConfig.ActionPoint
            };
            ProcessAction(action);

            return ActionBase.EveryRender<DailyReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public IObservable<ActionBase.ActionEvaluation<ItemEnhancement>> ItemEnhancement(Guid itemId, IEnumerable<Guid> materialIds)
        {
            var action = new ItemEnhancement
            {
                itemId = itemId,
                materialIds = materialIds,
                avatarAddress = States.Instance.CurrentAvatarState.address,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<ItemEnhancement>()
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

            return ActionBase.EveryRender<QuestReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public static IObservable<ActionBase.ActionEvaluation<RankingBattle>> RankingBattle(Address enemyAddress)
        {
            if (!ArenaHelper.TryGetThisWeekAddress(out var weeklyArenaAddress))
                throw new NullReferenceException(nameof(weeklyArenaAddress));
            
            var action = new RankingBattle
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                EnemyAddress = enemyAddress,
                WeeklyArenaAddress = weeklyArenaAddress,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<RankingBattle>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public static IObservable<ActionBase.ActionEvaluation<WeeklyArenaReward>> WeeklyArenaReward()
        {
            var action = new WeeklyArenaReward
            {
                AvatarAddress = States.Instance.CurrentAvatarState.address,
                WeeklyArenaAddress = ArenaHelper.GetPrevWeekAddress()
            };
            ProcessAction(action);

            return ActionBase.EveryRender<WeeklyArenaReward>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Timeout(ActionTimeout);
        }

        public static void PatchTableSheet(string tableName, string tableCsv)
        {
            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv,
            };
            ProcessAction(action);
        }

        #endregion
    }
}
