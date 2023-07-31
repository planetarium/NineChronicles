using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class ProductsState
    {
        public static Address DeriveAddress(Address avatarAddress) =>
            avatarAddress.Derive(nameof(ProductsState));

        public List<Guid> ProductIds = new List<Guid>();

        public ProductsState()
        {
        }

        public ProductsState(List serialized)
        {
            ProductIds = serialized.ToList(StateExtensions.ToGuid);
        }

        public IValue Serialize()
        {
            return ProductIds
                .Aggregate(
                    List.Empty,
                    (current, productId) => current.Add(productId.Serialize())
                );
        }
    }
}
