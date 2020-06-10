using System.Collections;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class FriendInfoPopup : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private Button blurButton = null;

        [SerializeField]
        private RectTransform modal = null;

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
        private Vector3 _previousAvatarLocalScale;
        private int _previousAvatarSortingLayerID;
        private int _previousAvatarSortingLayerOrder;
        private bool _previousAvatarActivated;
        private Coroutine _constraintsAvatarToUICoroutine;
        private CharacterStats _tempStats;

        #region Override

        protected override void Awake()
        {
            base.Awake();

            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);

            blurButton.OnClickAsObservable()
                .Subscribe(_ => Close())
                .AddTo(gameObject);
        }

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
            _previousAvatarActivated = stage.selectedPlayer && stage.selectedPlayer.gameObject.activeSelf;
            var player = stage.GetPlayer();
            player.Set(avatarState);
            var playerTransform = player.transform;
            _previousAvatarPosition = playerTransform.position;
            _previousAvatarLocalScale = playerTransform.localScale;
            _previousAvatarSortingLayerID = player.sortingGroup.sortingLayerID;
            _previousAvatarSortingLayerOrder = player.sortingGroup.sortingOrder;

            playerTransform.position = avatarPosition.position;
            var orderInLayer = MainCanvas.instance.GetLayer(WidgetType).root.sortingOrder + 1;
            player.SetSortingLayer(SortingLayer.NameToID("UI"), orderInLayer);

            _tempStats = player.Model.Stats.Clone() as CharacterStats;

            if (!(_constraintsAvatarToUICoroutine is null))
            {
                StopCoroutine(_constraintsAvatarToUICoroutine);
            }

            _constraintsAvatarToUICoroutine = StartCoroutine(CoConstraintsPlayerToUI(playerTransform));
        }

        private IEnumerator CoConstraintsPlayerToUI(Transform playerTransform)
        {
            while (true)
            {
                playerTransform.position = avatarPosition.position;
                playerTransform.localScale = modal.localScale;
                yield return null;
            }
        }

        private void ReturnPlayer()
        {
            if (!(_constraintsAvatarToUICoroutine is null))
            {
                StopCoroutine(_constraintsAvatarToUICoroutine);
                _constraintsAvatarToUICoroutine = null;
            }

            // NOTE: 플레이어를 강제로 재생성해서 플레이어의 모델이 장비 변경 상태를 반영하도록 합니다.
            var player = Game.Game.instance.Stage.GetPlayer(_previousAvatarPosition, true);
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            player.Set(currentAvatarState);
            player.SetSortingLayer(_previousAvatarSortingLayerID, _previousAvatarSortingLayerOrder);
            player.transform.localScale = _previousAvatarLocalScale;
            player.gameObject.SetActive(_previousAvatarActivated);
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
