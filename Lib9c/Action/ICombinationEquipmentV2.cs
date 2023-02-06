using Libplanet;

namespace Nekoyume.Action
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
