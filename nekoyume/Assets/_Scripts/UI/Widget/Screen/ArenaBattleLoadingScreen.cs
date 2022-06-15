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

        private static readonly int Close1 = Animator.StringToHash("Close");

        public void Show(
            string playerName,
            int playerLevel,
            int playerFullCostumeOrArmorId,
            string enemyName,
            int enemyLevel,
            int enemyFullCostumeOrArmorId)
        {
            var playerSprite = SpriteHelper.GetItemIcon(playerFullCostumeOrArmorId);
            playerProfile.Set(playerLevel, playerName, playerSprite);
            var enemySprite = SpriteHelper.GetItemIcon(enemyFullCostumeOrArmorId);
            enemyProfile.Set(enemyLevel, enemyName, enemySprite);
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
