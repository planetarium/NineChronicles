using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IEventConsumableItemCraftsV1
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventConsumableItemRecipeId { get; }
        int SlotIndex { get; }
    }
}
