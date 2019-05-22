using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.State;
using NetMQ;
using UniRx;
using UnityEngine;

namespace Nekoyume
{
    /// <summary>
    /// 게임의 Action을 생성하고 Agent에 넣어주는 역할을 한다.
    /// </summary>
    public class ActionManager : MonoSingleton<ActionManager>
    {
        private static void ProcessAction(GameAction action)
        {
            action.Id = action.Id.Equals(default(Guid)) ? Guid.NewGuid() : action.Id;
            AgentController.Agent.EnqueueAction(action);
        }
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
        
        protected override void OnDestroy() 
        {
            NetMQConfig.Cleanup(false);
            
            base.OnDestroy();
        }
        
        #endregion
        
        #region Actions
        
        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice
            {
                name = nickName,
                avatarAddress = AddressBook.Avatar.Value,
            };
            ProcessAction(action);
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
            return Action.HackAndSlash.EveryRender<HackAndSlash>().SkipWhile(
                eval => !eval.Action.Id.Equals(action.Id)
            ).Take(1).Last();  // Last() is for completion
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

        public IObservable<Sell.ResultModel> Sell(int itemId, int count, decimal price)
        {
            var action = new Sell {itemId = itemId, count = count, price = price};
            ProcessAction(action);

            return ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var result = eval.Action.result;
                    
                    // 인벤토리에서 빼기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.RemoveEquipmentItemFromItems(result.shopItem.item.Data.id, result.shopItem.count);
                    
                    // 상점에 넣기. ReactiveShopState 에서 동기화 중.
//                    States.Shop.Value.Register(AddressBook.Avatar.Value, result.shopItem);

                    return result;
                }); // Last() is for completion
        }
        
        public IObservable<SellCancellation.ResultModel> SellCancellation(Address owner, Guid productId)
        {
            var action = new SellCancellation {owner = owner, productId = productId};
            ProcessAction(action);

            return ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var result = eval.Action.result;
                    
                    // 상점에서 빼기. ReactiveShopState 에서 동기화 중.
//                    var shopItem = States.Shop.Value.Unregister(result.owner, result.shopItem.productId);
                    // 인벤토리에 넣기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.AddEquipmentItemToItems(shopItem.item.Data.id, shopItem.count);

                    return result;
                }); // Last() is for completion
        }
        
        public IObservable<Buy.ResultModel> Buy(Address owner, Guid productId)
        {
            var action = new Buy {owner = owner, productId = productId};
            ProcessAction(action);

            return ActionBase.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var result = eval.Action.result;
                    
                    // 상점에서 빼기. ReactiveShopState 에서 동기화 중.
//                    var shopItem = States.Shop.Value.Unregister(result.owner, result.shopItem.productId);
                    // 인벤토리에 넣기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.AddEquipmentItemToItems(shopItem.item.Data.id, shopItem.count);

                    return result;
                }); // Last() is for completion
        }
        
        #endregion
    }
}
