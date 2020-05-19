using Nekoyume.Battle;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AvatarInfo : Widget
    {
        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        // TODO: Costume 슬롯 대응하기.
        // [SerializeField]
        // private EquipmentSlots costumeSlots = null;

        // TODO: Rename equipmentSlots.
        [SerializeField]
        private EquipmentSlots slots = null;

        [SerializeField]
        private DetailedStatView[] statViews = null;


        private const string nicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        #region Override

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var gameInstance = Game.Game.instance;
            var player = gameInstance.Stage.selectedPlayer.Model;

            var statTuples = player.Stats.GetBaseAndAdditionalStats();
            var idx = 0;
            foreach (var (statType, value, additionalValue) in statTuples)
            {
                var info = statViews[idx];
                info.Show(statType, value, additionalValue);
                ++idx;
            }

            var currentAvatar = gameInstance.States.CurrentAvatarState;

            nicknameText.text = string.Format(
                nicknameTextFormat,
                currentAvatar.level,
                currentAvatar.NameWithHash);

            cpText.text = CPHelper.GetCP(currentAvatar, gameInstance.TableSheets.CharacterSheet).ToString();

            // TODO: Costume 슬롯 대응하기.
            // slots.SetPlayerCostumes(player, null, null);
            slots.SetPlayerEquipments(player, null, null);
        }

        #endregion
    }
}
