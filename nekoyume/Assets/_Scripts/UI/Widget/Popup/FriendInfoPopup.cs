using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
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

        private static readonly Vector3 NPCPosition = new Vector3(2000f, 1999.2f, 2.15f);
        private static readonly Vector3 NPCPositionInLobbyCamera = new Vector3(5000f, 4999.13f, 0f);

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private Transform titleSocket = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private AvatarStats avatarStats = null;

        [SerializeField]
        private RawImage playerRawImage;

        [SerializeField]
        private RawImage playerRawImageInLobbyCamera;

        private CharacterStats _tempStats;
        private GameObject _cachedCharacterTitle;
        private Player _player;

        #region Override

        protected override void Awake()
        {
            base.Awake();

            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            Show(currentAvatarState, ignoreShowAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            TerminatePlayer();
        }

        #endregion

        public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            InitializePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private void InitializePlayer(AvatarState avatarState)
        {
            _player = PlayerFactory.Create(avatarState).GetComponent<Player>();
            var t = _player.transform;
            t.localScale = Vector3.one;

            var playerInLobby = Find<Menu>().isActiveAndEnabled;
            if (playerInLobby)
            {
                t.position = NPCPosition;
                playerRawImage.gameObject.SetActive(true);
                playerRawImageInLobbyCamera.gameObject.SetActive(false);
            }
            else
            {
                t.position = NPCPositionInLobbyCamera;
                playerRawImage.gameObject.SetActive(false);
                playerRawImageInLobbyCamera.gameObject.SetActive(true);
            }
        }

        private void TerminatePlayer()
        {
            var t = _player.transform;
            t.SetParent(Game.Game.instance.Stage.transform);
            t.localScale = Vector3.one;
            _player.gameObject.SetActive(false);
            _player = null;
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = _player.Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);

            if (!(title is null))
            {
                Destroy(_cachedCharacterTitle);
                var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                    title.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            cpText.text = CPHelper
                .GetCPV2(avatarState, game.TableSheets.CharacterSheet,
                    game.TableSheets.CostumeStatSheet)
                .ToString();

            costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, null);
        }

        private void UpdateStatViews()
        {
            _tempStats = _player.Model.Stats.Clone() as CharacterStats;
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var costumes = costumeSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Costume)
                .Where(item => !(item is null))
                .ToList();

            var equipEffectSheet = Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var stats = _tempStats.SetAll(_tempStats.Level, equipments, costumes, null,
                equipEffectSheet, costumeSheet);
            avatarStats.SetData(stats);
        }

        private static void ShowTooltip(EquipmentSlot slot)
        {
            var item = new InventoryItem(slot.Item, 1, true, false, true);
            var tooltip = ItemTooltip.GetItemTooltipByItemType(item.ItemBase.ItemType);
            tooltip.Show(slot.RectTransform, item, string.Empty, false, null);
        }
    }
}
