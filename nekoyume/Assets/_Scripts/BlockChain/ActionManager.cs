using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Manager;
using Nekoyume.Pattern;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager : MonoSingleton<ActionManager>
    {
        private static void ProcessAction(GameAction action)
        {
            AgentController.Agent.EnqueueAction(action);
        }

        #region Actions

        public IObservable<ActionBase.ActionEvaluation<CreateAvatar>> CreateAvatar(Address avatarAddress, int index,
            string nickName)
        {
            var action = new CreateAvatar
            {
                avatarAddress = avatarAddress,
                index = index,
                name = nickName,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<CreateAvatar>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }

        public IObservable<ActionBase.ActionEvaluation<DeleteAvatar>> DeleteAvatar(int index)
        {
            var avatarAddress = States.Instance.avatarStates[index].address;
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
                .ObserveOnMainThread();
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Equipment> equipments,
            List<Food> foods,
            int stage)
        {
            var action = new HackAndSlash
            {
                equipments = equipments,
                foods = foods,
                stage = stage,
                avatarAddress = States.Instance.currentAvatarState.Value.address,
            };
            ProcessAction(action);

            var itemIDs = equipments.Select(e => e.Data.id).Concat(foods.Select(f => f.Data.id)).ToArray();
            AnalyticsManager.Instance.Battle(itemIDs);
            return ActionBase.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }

        public IObservable<ActionBase.ActionEvaluation<Action.Combination>> Combination(
            List<CombinationMaterial> materials)
        {
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombination);

            var action = new Action.Combination();
            materials.ForEach(m => action.Materials.Add(new Action.Combination.Material(m)));
            action.avatarAddress = States.Instance.currentAvatarState.Value.address;
            ProcessAction(action);

            return ActionBase.EveryRender<Action.Combination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(ItemUsable itemUsable, decimal price)
        {
            var action = new Sell
            {
                sellerAvatarAddress = States.Instance.currentAvatarState.Value.address,
                productId = Guid.NewGuid(),
                itemUsable = itemUsable,
                price = price
            };
            ProcessAction(action);

            return ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<SellCancellation>> SellCancellation(Address sellerAvatarAddress,
            Guid productId)
        {
            var action = new SellCancellation
            {
                productId = productId,
                sellerAvatarAddress = States.Instance.currentAvatarState.Value.address,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<Buy>> Buy(Address sellerAgentAddress,
            Address sellerAvatarAddress, Guid productId)
        {
            var action = new Buy
            {
                buyerAvatarAddress = States.Instance.currentAvatarState.Value.address,
                sellerAgentAddress = sellerAgentAddress,
                sellerAvatarAddress = sellerAvatarAddress,
                productId = productId
            };
            ProcessAction(action);

            return ActionBase.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<AddItem>> AddItem(Guid itemId)
        {
            var action = new AddItem
            {
                avatarAddress = States.Instance.currentAvatarState.Value.address,
                itemId = itemId,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<AddItem>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }

        public IObservable<ActionBase.ActionEvaluation<AddGold>> AddGold()
        {
            var action = new AddGold
            {
                agentAddress = States.Instance.agentState.Value.address,
                avatarAddress = States.Instance.currentAvatarState.Value.address,
            };
            ProcessAction(action);

            return ActionBase.EveryRender<AddGold>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion

        }

        #endregion
    }
}
