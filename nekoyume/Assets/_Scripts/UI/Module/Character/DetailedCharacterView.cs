using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedCharacterView : FramedCharacterView
    {
        [SerializeField]
        private TextMeshProUGUI levelText = null;

        public override void SetByAvatarState(AvatarState avatarState)
        {
            base.SetByAvatarState(avatarState);
            levelText.text = avatarState.level.ToString();
        }

        public override void SetByPlayer(Player player)
        {
            base.SetByPlayer(player);
            levelText.text = player.Level.ToString();
        }

        public void SetByFullCostumeOrArmorId(int armorId, int level) =>
            SetByFullCostumeOrArmorId(armorId, level.ToString());

        public void SetByFullCostumeOrArmorId(int armorId, string level)
        {
            SetByFullCostumeOrArmorId(armorId);
            levelText.text = level;
        }

        public void SetByArenaInfo(ArenaInfo arenaInfo)
        {
            SetByFullCostumeOrArmorId(arenaInfo.ArmorId);
            levelText.text = $"Lv.{arenaInfo.Level}";
        }
    }
}
