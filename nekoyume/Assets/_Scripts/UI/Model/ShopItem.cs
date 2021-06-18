using System;
using System.Numerics;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<Address> SellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> SellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<FungibleAssetValue> Price = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<Guid> OrderId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<Guid> TradableId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<ItemSubType> ItemSubType = new ReactiveProperty<ItemSubType>();
        public readonly ReactiveProperty<long> ExpiredBlockIndex = new ReactiveProperty<long>();
        public readonly ReactiveProperty<int> Level = new ReactiveProperty<int>();

        public ShopItemView View;

        public ShopItem(OrderDigest orderDigest) : this(orderDigest, GetItemBase(orderDigest.ItemId))
        {
        }

        private ShopItem(OrderDigest orderDigest, ItemBase item) : base(item, 1)
        {
            GradeEnabled.Value = true;
            SellerAgentAddress.Value = orderDigest.SellerAgentAddress;
            SellerAvatarAddress.Value = orderDigest.SellerAgentAddress; // todo : 아바타 넣어줘야됨
            Price.Value = orderDigest.Price;
            Count.Value = orderDigest.ItemCount;
            OrderId.Value = orderDigest.OrderId;
            TradableId.Value = orderDigest.TradableId;
            ExpiredBlockIndex.Value = orderDigest.ExpiredBlockIndex;
            Level.Value = orderDigest.Level;
            ItemSubType.Value = item.ItemSubType;
        }

        public override void Dispose()
        {
            SellerAgentAddress.Dispose();
            SellerAvatarAddress.Dispose();
            Price.Dispose();
            OrderId.Dispose();
            base.Dispose();
        }

        private static ItemBase GetItemBase(int itemId)
        {
            var row = Game.Game.instance.TableSheets.ItemSheet[itemId];
            var item = ItemFactory.CreateItem(row, new Cheat.DebugRandom());
            return item;
        }
    }
}
