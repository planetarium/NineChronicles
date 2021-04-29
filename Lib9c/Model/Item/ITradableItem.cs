using System;

namespace Nekoyume.Model.Item
{
    public interface ITradableItem: IItem
    {
        Guid TradableId { get; }
    }
}
