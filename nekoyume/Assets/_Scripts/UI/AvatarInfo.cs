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

        [SerializeField]
        private RectTransform avatarPosition = null;

        private const string nicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        private Vector3 _previousAvatarPosition;

        #region Override

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var gameInstance = Game.Game.instance;
            var stage = gameInstance.Stage;
            var playerModel = stage.selectedPlayer.Model;

            var statTuples = playerModel.Stats.GetBaseAndAdditionalStats();
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
            // slots.SetPlayerCostumes(playerModel, null, null);
            slots.SetPlayerEquipments(playerModel, null, null);

            var player = stage.GetPlayer();
            _previousAvatarPosition = player.transform.position;
            player.transform.position = avatarPosition.position;
            var layerId = SortingLayer.NameToID("UI");
            player.SetSortingLayer(layerId, 1);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            var stage = Game.Game.instance.Stage;
            var player = stage.GetPlayer(_previousAvatarPosition);
            var layerId = SortingLayer.NameToID("Character");
            player.SetSortingLayer(layerId, 100);
        }

        #endregion
    }
}
