using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaBattleLoadingScreen : ScreenWidget
    {
        [SerializeField]
        private CharacterProfile playerProfile = null;

        [SerializeField]
        private CharacterProfile enemyProfile = null;

        [SerializeField]
        private TextMeshProUGUI loadingText = null;

        private Player player;
        private static readonly int Close1 = Animator.StringToHash("Close");

        public void Show(ArenaInfo enemyInfo) =>
            Show(enemyInfo.AvatarName, enemyInfo.Level, enemyInfo.ArmorId);

        public void Show(string avatarName, int level, int fullCostumeOrArmorId)
        {
            player = Game.Game.instance.Stage.GetPlayer();
            var sprite =
                SpriteHelper.GetItemIcon(player.Model.armor?.Id ?? GameConfig.DefaultAvatarArmorId);
            playerProfile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
            player.gameObject.SetActive(false);
            var enemySprite = SpriteHelper.GetItemIcon(fullCostumeOrArmorId);
            enemyProfile.Set(level, avatarName, enemySprite);
            loadingText.text = L10nManager.Localize("UI_MATCHING_OPPONENT");
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Animator.SetTrigger(Close1);
            base.Close(ignoreCloseAnimation);
        }
    }
}
