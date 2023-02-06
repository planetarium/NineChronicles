using Libplanet;

namespace Nekoyume.Action
{
    public interface ICombinationConsumableV1
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
    }
}
