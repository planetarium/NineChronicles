using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager : MonoSingleton<ActionManager>
    {
        private static void ProcessAction(GameAction action)
        {
//            action.Id = action.Id.Equals(default(Guid)) ? Guid.NewGuid() : action.Id;
            AgentController.Agent.EnqueueAction(action);
        }
        
        #region Actions
        
        public IObservable<ActionBase.ActionEvaluation<CreateAvatar>> CreateNovice(Address avatarAddress, int index, string nickName)
        {
            var action = AgentController.Agent.CreateAvatar(avatarAddress, index, nickName);
            
            return ActionBase.EveryRender<CreateAvatar>()
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
                Equipments = equipments,
                Foods = foods,
                Stage = stage,
            };
            ProcessAction(action);

            var itemIDs = equipments.Select(e => e.Data.id).Concat(foods.Select(f => f.Data.id)).ToArray();
            AnalyticsManager.instance.Battle(itemIDs);
            return ActionBase.EveryRender<HackAndSlash>()
                .SkipWhile(eval => !eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }
        
        public IObservable<ActionBase.ActionEvaluation<Combination>> Combination(
            List<UI.Model.CountEditableItem> materials)
        {
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombination);
            
            var action = new Combination();
            materials.ForEach(m => action.Materials.Add(new Combination.ItemModel(m)));
            ProcessAction(action);

            return ActionBase.EveryRender<Combination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }

        public IObservable<ActionBase.ActionEvaluation<Sell>> Sell(int itemId, int count, decimal price)
        {
            var action = new Sell
            {
                sellerAgentAddress = States.AgentState.Value.address,
                productId = Guid.NewGuid(),
                itemId = itemId,
                count = count,
                price = price
            };
            ProcessAction(action);

            return ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }
        
        public IObservable<ActionBase.ActionEvaluation<SellCancellation>> SellCancellation(Address sellerAvatarAddress, Guid productId)
        {
            var action = new SellCancellation
            {
                sellerAvatarAddress = sellerAvatarAddress,
                productId = productId
            };
            ProcessAction(action);

            return ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread(); // Last() is for completion
        }
        
        public IObservable<ActionBase.ActionEvaluation<Buy>> Buy(Address sellerAgentAddress, Address sellerAvatarAddress, Guid productId)
        {
            var action = new Buy
            {
                buyerAgentAddress = States.AgentState.Value.address,
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
        
        #endregion
    }
}
