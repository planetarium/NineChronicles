using Libplanet;

namespace Nekoyume.Action
{
    public interface ICombinationEquipmentV3
    {
        Address AvatarAddress { get; }
        int RecipeId { get; }
        int SlotIndex { get; }
        int? SubRecipeId { get; }
        bool PayByCrystal { get; }
        bool UseHammerPoint { get; }
    }
}
