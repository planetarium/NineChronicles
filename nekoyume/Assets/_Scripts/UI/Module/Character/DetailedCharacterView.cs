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
            levelText.text = $"Lv.{avatarState.level}";
        }

        public override void SetByPlayer(Player player)
        {
            base.SetByPlayer(player);
            levelText.text = $"Lv.{player.Level}";
        }

        public void SetByFullCostumeOrArmorId(int armorId, int? titleId, int level)
        {
            SetByFullCostumeOrArmorId(armorId);
            if (titleId.HasValue)
            {
                var sprite = SpriteHelper.GetTitleFrame(titleId.Value);
                SetFrame(sprite);
            }
            levelText.text = $"Lv.{level}";
        }

        public void SetByArenaInfo(ArenaInfo arenaInfo)
        {
            SetByFullCostumeOrArmorId(arenaInfo.ArmorId);
            levelText.text = $"Lv.{arenaInfo.Level}";
        }
    }
}
