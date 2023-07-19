using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Market;

namespace Nekoyume.Action
{
    public interface IProductInfo
    {
        public Guid ProductId { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Address AgentAddress { get; set; }
        public Address AvatarAddress { get; set; }
        public ProductType Type { get; set; }

        public IValue Serialize();

        public void ValidateType();
    }
}
