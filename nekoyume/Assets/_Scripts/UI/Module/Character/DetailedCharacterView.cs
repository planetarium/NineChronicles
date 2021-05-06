using Nekoyume.Game.Character;
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
            levelText.text = $"Lv.{avatarState.level}";
        }

        public override void SetByPlayer(Player player)
        {
            base.SetByPlayer(player);
            levelText.text = $"Lv.{player.Level}";
        }

        public void SetByArenaInfo(ArenaInfo arenaInfo)
        {
            SetByArmorId(arenaInfo.ArmorId);
            levelText.text = $"Lv.{arenaInfo.Level}";
        }
    }
}
