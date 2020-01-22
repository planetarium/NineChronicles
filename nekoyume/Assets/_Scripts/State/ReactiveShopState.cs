using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.State
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        public static readonly ReactiveProperty<Dictionary<Address, List<ShopItem>>> Items =
            new ReactiveProperty<Dictionary<Address, List<ShopItem>>>();

        public static void Initialize(ShopState state)
        {
            if (state is null)
                return;

            Items.Value = state.AgentProducts;
        }
    }
}
