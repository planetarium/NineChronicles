using Libplanet.Crypto;
using Nekoyume.L10n;
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
            Address playerAvatarAddress,
            string enemyName,
            int enemyLevel,
            int enemyFullCostumeOrArmorId,
            Address enemyAvatarAddress)
        {
            playerProfile.Set(playerLevel, playerName, playerFullCostumeOrArmorId, playerAvatarAddress);
            enemyProfile.Set(enemyLevel, enemyName, enemyFullCostumeOrArmorId, enemyAvatarAddress);
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
