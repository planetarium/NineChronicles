using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class RankingBattleLoadingScreen : ScreenWidget
    {
        public CharacterProfile profile;
        public CharacterProfile enemyProfile;

        public void Show(ArenaInfo enemyInfo)
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            var sprite = SpriteHelper.GetItemIcon(player.Model.armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId);
            profile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
            var enemySprite = SpriteHelper.GetItemIcon(enemyInfo.ArmorId);
            enemyProfile.Set(enemyInfo.Level, enemyInfo.AvatarName, enemySprite);

            base.Show();
        }
    }
}
