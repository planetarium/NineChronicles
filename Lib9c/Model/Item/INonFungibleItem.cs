using System;

namespace Nekoyume.Model.Item
{
    public interface INonFungibleItem: IItem
    {
        Guid NonFungibleId { get; }
        long RequiredBlockIndex { get; set; }
    }
}
