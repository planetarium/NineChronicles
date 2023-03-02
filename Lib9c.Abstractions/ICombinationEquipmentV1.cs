using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ICombinationEquipmentV1
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
        int? SubRecipeId { get; }
    }
}
