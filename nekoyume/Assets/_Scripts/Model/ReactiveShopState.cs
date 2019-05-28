using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 ActionBase.EveryRender<T>()를 통해 감지하고, 동기화한다.
    /// 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        public static ReactiveDictionary<Address, List<ShopItem>> Items { get; private set; }
        
        static ReactiveShopState()
        {
            Subscribes();
        }
        
        public static void Initialize(ShopState shopState)
        {
            if (ReferenceEquals(shopState, null))
            {
                return;
            }
            
            Items = new ReactiveDictionary<Address, List<ShopItem>>(shopState.items);
        }
        
        private static void Subscribes()
        {
            ActionBase.EveryRender<Sell>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Register(Items, States.CurrentAvatarState.Value.address, result.shopItem);
                });
            
            ActionBase.EveryRender<SellCancellation>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Unregister(Items, result.owner, result.shopItem.productId);
                });
            
            ActionBase.EveryRender<Buy>()
                .Where(eval => eval.InputContext.Signer == States.CurrentAvatarState.Value.address
                               && eval.Action.errorCode == GameActionErrorCode.Success)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    var result = eval.Action.result;
                    ShopState.Unregister(Items, result.owner, result.shopItem.productId);
                });
        }
    }
}
