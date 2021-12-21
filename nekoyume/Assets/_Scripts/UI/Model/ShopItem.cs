using System;
using Lib9c.Model.Order;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<FungibleAssetValue> Price = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<Guid> OrderId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<Guid> TradableId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<long> ExpiredBlockIndex = new ReactiveProperty<long>();
        public readonly ReactiveProperty<int> Level = new ReactiveProperty<int>();

        public ShopItemView View;

        public ShopItem(OrderDigest orderDigest, ItemBase item) : base(item, orderDigest.ItemCount)
        {
            GradeEnabled.Value = true;
            Price.Value = orderDigest.Price;
            OrderId.Value = orderDigest.OrderId;
            TradableId.Value = orderDigest.TradableId;
            ExpiredBlockIndex.Value = orderDigest.ExpiredBlockIndex;
            Level.Value = orderDigest.Level;
        }

        public override void Dispose()
        {
            Price.Dispose();
            OrderId.Dispose();
            TradableId.Dispose();
            ExpiredBlockIndex.Dispose();
            Level.Dispose();
            base.Dispose();
        }
    }
}
