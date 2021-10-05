using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class AvatarExtensions
    {
        public static bool TryGetEquippedFullCostume(this AvatarState avatarState, out Costume fullCostume)
        {
            fullCostume = null;

            if (avatarState?.inventory is null)
            {
                return false;
            }

            var inventory = avatarState.inventory;
            fullCostume = inventory.Costumes.FirstOrDefault(e =>
                e.ItemSubType == ItemSubType.FullCostume &&
                e.Equipped);
            return fullCostume is { };
        }

        public static (bool isCharacterId, int id) GetPortraitId(
            this AvatarState avatarState,
            bool getCharacterIdWhenArmorIsEmpty = false)
        {
            var portraitId = TryGetEquippedFullCostume(avatarState, out var fullCostume)
                ? fullCostume.Id
                : avatarState.GetArmorId();

            if (getCharacterIdWhenArmorIsEmpty)
            {
                return portraitId == GameConfig.DefaultAvatarArmorId
                    ? (true, avatarState.characterId)
                    : (false, portraitId);
            }

            return (false, portraitId);
        }

        public static int GetCP(this AvatarState avatarState)
        {
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            return CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
        }
    }
}
