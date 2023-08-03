using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface ICombinationEquipmentV4
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
        int? SubRecipeId { get; }
        bool PayByCrystal { get; }
        bool UseHammerPoint { get; }
        int? PetId { get; }
    }
}
