using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ICombinationConsumableV1
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
    }
}
