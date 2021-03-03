using System;

namespace Nekoyume.Model.Item
{
    public interface INonFungibleItem
    {
        Guid ItemId { get; }
        long RequiredBlockIndex { get; }
    }
}
