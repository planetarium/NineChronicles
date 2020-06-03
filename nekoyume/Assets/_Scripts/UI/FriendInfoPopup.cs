using System.Collections;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class FriendInfoPopup : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private AvatarStats avatarStats = null;

        [SerializeField]
        private RectTransform avatarPosition = null;

        private Vector3 _previousAvatarPosition;
        private int _previousSortingLayerID;
        private int _previousSortingLayerOrder;
        private bool _previousActivated;
        private CharacterStats _tempStats;
        private Coroutine _constraintsPlayerToUI;

        #region Override

        public override void Show(bool ignoreShowAnimation = false)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            Show(currentAvatarState, ignoreShowAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            ReturnPlayer();
        }

        #endregion

        public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            ReplacePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private void ReplacePlayer(AvatarState avatarState)
        {
            var stage = Game.Game.instance.Stage;
            _previousActivated = stage.selectedPlayer && stage.selectedPlayer.gameObject.activeSelf;
            var player = stage.GetPlayer();
            player.Set(avatarState);
            var playerTransform = player.transform;
            _previousAvatarPosition = playerTransform.position;
            _previousSortingLayerID = player.sortingGroup.sortingLayerID;
            _previousSortingLayerOrder = player.sortingGroup.sortingOrder;

            playerTransform.position = avatarPosition.position;
            player.SetSortingLayer(SortingLayer.NameToID("UI"), 11);

            _tempStats = player.Model.Stats.Clone() as CharacterStats;

            if (!(_constraintsPlayerToUI is null))
            {
                StopCoroutine(_constraintsPlayerToUI);
            }

            _constraintsPlayerToUI = StartCoroutine(CoConstraintsPlayerToUI(playerTransform));
        }

        private IEnumerator CoConstraintsPlayerToUI(Transform playerTransform)
        {
            while (enabled)
            {
                playerTransform.position = avatarPosition.position;
                yield return null;
            }
        }

        private void ReturnPlayer()
        {
            if (!(_constraintsPlayerToUI is null))
            {
                StopCoroutine(_constraintsPlayerToUI);
                _constraintsPlayerToUI = null;
            }

            // NOTE: 플레이어를 강제로 재생성해서 플레이어의 모델이 장비 변경 상태를 반영하도록 합니다.
            var player = Game.Game.instance.Stage.GetPlayer(_previousAvatarPosition, true);
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            player.Set(currentAvatarState);
            player.SetSortingLayer(_previousSortingLayerID, _previousSortingLayerOrder);
            player.gameObject.SetActive(_previousActivated);
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = game.Stage.GetPlayer().Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            cpText.text = CPHelper
                .GetCP(avatarState, game.TableSheets.CharacterSheet)
                .ToString();

            costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, null);
        }

        private void UpdateStatViews()
        {
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                null,
                Game.Game.instance.TableSheets
            );

            avatarStats.SetData(stats);
        }

        private static void ShowTooltip(EquipmentSlot slot)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (slot is null ||
                slot.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            tooltip.Show(slot.RectTransform, new InventoryItem(slot.Item, 1));
        }
    }
}
