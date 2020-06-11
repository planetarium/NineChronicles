using Nekoyume.Game.Character;
using Nekoyume.Helper;
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

        public void Show(ArenaInfo enemyInfo)
        {
            player = Game.Game.instance.Stage.GetPlayer();
            var sprite = SpriteHelper.GetItemIcon(player.Model.armor? .Id ?? GameConfig.DefaultAvatarArmorId);
            playerProfile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
            player.gameObject.SetActive(false);
            var enemySprite = SpriteHelper.GetItemIcon(enemyInfo.ArmorId);
            enemyProfile.Set(enemyInfo.Level, enemyInfo.AvatarName, enemySprite);
            Show();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            player.gameObject.SetActive(true);
        }
    }
}
