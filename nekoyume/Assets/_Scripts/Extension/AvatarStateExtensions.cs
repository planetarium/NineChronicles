using System.Linq;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class AvatarStateExtensions
    {
        public static ArenaAvatarState ToArenaAvatarState(this AvatarState avatarState)
        {
            var arenaAvatarState = new ArenaAvatarState(avatarState);
            arenaAvatarState.UpdateCostumes(avatarState.inventory.Costumes
                .Select(e => e.NonFungibleId)
                .ToList());
            arenaAvatarState.UpdateEquipment(avatarState.inventory.Equipments
                .Select(e => e.NonFungibleId)
                .ToList());
            return arenaAvatarState;
        }

        public static AvatarState ApplyToInventory(
            this AvatarState avatarState,
            ArenaAvatarState arenaAvatarState)
        {
            avatarState.inventory = avatarState.inventory.Apply(arenaAvatarState);
            return avatarState;
        }

        public static AvatarState CloneAndApplyToInventory(
            this AvatarState avatarState,
            ArenaAvatarState arenaAvatarState) =>
            new AvatarState(avatarState)
            {
                inventory = avatarState.inventory.CloneAndApply(arenaAvatarState),
            };
    }
}
