using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        public static ReactiveDictionary<Address, List<ShopItem>> Items { get; private set; }
        
        public static void Initialize(ShopState shopState)
        {
            if (ReferenceEquals(shopState, null))
            {
                return;
            }
            
            Items = new ReactiveDictionary<Address, List<ShopItem>>(shopState.items);
        }
    }
}
