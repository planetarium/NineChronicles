using System;

namespace Nekoyume.Model.Item
{
    public interface INonFungibleItem: ITradableItem
    {
        Guid NonFungibleId { get; }
        long RequiredBlockIndex { get; }
        void Update(long blockIndex);
    }
}
