using System.Collections;
using System.Linq;
using Nekoyume.Battle;
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

        [SerializeField]
        private Button blurButton = null;

        [SerializeField]
        private RectTransform modal = null;

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
        private RectTransform avatarPosition = null;

        private CharacterStats _tempStats;
        private GameObject _cachedCharacterTitle;
        private Player _player;

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

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _player.transform.SetParent(Game.Game.instance.Stage.transform);
            _player.transform.localScale = Vector3.one;
            _player.gameObject.SetActive(false);
            base.Close(ignoreCloseAnimation);
        }
        #endregion

        public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            CreatePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private void CreatePlayer(AvatarState avatarState)
        {
            var orderInLayer = MainCanvas.instance.GetLayer(WidgetType).root.sortingOrder + 1;
            _player = PlayerFactory.CreateBySettingLayer(avatarState, SortingLayer.NameToID("UI"), orderInLayer)
                .GetComponent<Player>();
            _player.Set(avatarState);
            _player.transform.SetParent(avatarPosition);
            _player.transform.localScale = Vector3.one * 160.0f;
            _player.transform.localPosition = Vector3.zero;
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = game.Stage.GetPlayer().Model;

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
                var clone  = ResourcesHelper.GetCharacterTitle(title.Grade, title.GetLocalizedNonColoredName());
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            cpText.text = CPHelper
                .GetCPV2(avatarState, game.TableSheets.CharacterSheet, game.TableSheets.CostumeStatSheet)
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

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                null,
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet
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
