using System;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class ItemProductInfo : IProductInfo
    {
        public Guid ProductId { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Address AgentAddress { get; set; }
        public Address AvatarAddress { get; set; }
        public ProductType Type { get; set; }
        public bool Legacy { get; set; }
        public ItemSubType ItemSubType { get; set; }
        public Guid TradableId { get; set; }

        public ItemProductInfo()
        {
        }

        public ItemProductInfo(List serialized)
        {
            ProductId = serialized[0].ToGuid();
            Price = serialized[1].ToFungibleAssetValue();
            AgentAddress = serialized[2].ToAddress();
            AvatarAddress = serialized[3].ToAddress();
            Type = serialized[4].ToEnum<ProductType>();
            Legacy = serialized[5].ToBoolean();
            ItemSubType = serialized[6].ToEnum<ItemSubType>();
            TradableId = serialized[7].ToGuid();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(ProductId.Serialize())
                .Add(Price.Serialize())
                .Add(AgentAddress.Serialize())
                .Add(AvatarAddress.Serialize())
                .Add(Type.Serialize())
                .Add(Legacy.Serialize())
                .Add(ItemSubType.Serialize())
                .Add(TradableId.Serialize());
        }

        public void ValidateType()
        {
            if (Type == ProductType.FungibleAssetValue)
            {
                throw new InvalidProductTypeException(
                    $"{nameof(ItemProductInfo)} does not support {Type}");
            }
        }
    }
}
