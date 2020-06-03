using DG.Tweening;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

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

        public void Show(ArenaInfo enemyInfo)
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            var sprite = SpriteHelper.GetItemIcon(player.Model.armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId);
            playerProfile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
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
    }
}
