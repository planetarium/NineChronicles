using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        public static readonly ReactiveProperty<IReadOnlyDictionary<Address, List<ShopItem>>>
            AgentProducts = new ReactiveProperty<IReadOnlyDictionary<Address, List<ShopItem>>>();

        public static readonly ReactiveProperty<IReadOnlyDictionary<ItemSubType, List<ShopItem>>>
            ItemSubTypeProducts =
                new ReactiveProperty<IReadOnlyDictionary<ItemSubType, List<ShopItem>>>();

        public static void Initialize(ShopState state)
        {
            if (state is null)
            {
                return;
            }

            AgentProducts.Value = state.AgentProducts.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Select(item => state.Products[item])
                    .ToList());

            ItemSubTypeProducts.Value = state.ItemSubTypeProducts.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Select(item => state.Products[item])
                    .ToList());
        }
    }
}
