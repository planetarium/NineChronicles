using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ICombinationEquipmentV2
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
        int? SubRecipeId { get; }
        bool PayByCrystal { get; }
    }
}
