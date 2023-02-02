using Libplanet;

namespace Nekoyume.Action
{
    public interface ICombinationEquipmentV1
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
        int? SubRecipeId { get; }
    }
}
