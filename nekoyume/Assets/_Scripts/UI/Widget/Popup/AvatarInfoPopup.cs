using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class AvatarInfoPopup : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField]
        private CategoryTabButton adventureButton;

        [SerializeField]
        private CategoryTabButton arenaButton;

        [SerializeField]
        private CategoryTabButton raidButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button dccSlotButton;

        [SerializeField]
        private Button activeDccButton;

        [SerializeField]
        private Button activeCostumeButton;

        [SerializeField]
        private Toggle grindModeToggle;

        [SerializeField]
        private GrindModule grindModule;

        [SerializeField]
        private GameObject grindModePanel;

        [SerializeField]
        private GameObject statusObject;

        [SerializeField]
        private GameObject equipmentSlotObject;

        [SerializeField]
        private Image dccImage;

        [SerializeField]
        private Sprite activeSprite;

        [SerializeField]
        private Sprite disableSprite;

        [SerializeField]
        private Sprite dccSlotDefaultSprite;

        private Image _activeDcc;
        private Image _activeCostume;
        private BattlePreparation _battlePreparation;
        private ArenaBattlePreparation _arenaPreparation;
        private RaidPreparation _raidPreparation;
        private Grind _grind;
        private readonly ToggleGroup _toggleGroup = new();
        private readonly Dictionary<BattleType, System.Action> _onToggleCallback = new()
        {
            { BattleType.Adventure , null},
            { BattleType.Arena , null},
            { BattleType.Raid , null},
        };

        private int isActiveDcc = 0;
        private BattleType _currentBattleType = BattleType.Adventure;

        private readonly ReactiveProperty<bool> _isVisibleDcc = new();

        protected override void Awake()
        {
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(arenaButton);
            _toggleGroup.RegisterToggleable(raidButton);
            _activeDcc = activeDccButton.GetComponent<Image>();
            _activeCostume = activeCostumeButton.GetComponent<Image>();

            adventureButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);
                })
                .AddTo(gameObject);
            arenaButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Arena, _onToggleCallback[BattleType.Arena]);
                })
                .AddTo(gameObject);
            raidButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Raid, _onToggleCallback[BattleType.Raid]);
                })
                .AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            dccSlotButton.onClick.AddListener(() =>
            {
#if UNITY_ANDROID || UNITY_IOS
                Find<Alert>().Show("UI_RUN_ON_PC_TITLE","UI_RUN_ON_PC_CONTENT");
#else
                Find<DccSettingPopup>().Show();
#endif
            });

            activeDccButton.onClick.AddListener(() =>
            {
                Dcc.instance.SetVisible(_isVisibleDcc.Value ? 0 : 1);
                _isVisibleDcc.SetValueAndForceNotify(!_isVisibleDcc.Value);
                var msg = _isVisibleDcc.Value ? "UI_DCC_COSTUME_ACTIVATION" : "UI_DCC_COSTUME_DISABLED";
                NotificationSystem.Push(MailType.System, L10nManager.Localize(msg),
                    NotificationCell.NotificationType.Information);
            });

            activeCostumeButton.onClick.AddListener(() =>
            {
                Dcc.instance.SetVisible(_isVisibleDcc.Value ? 0 : 1);
                _isVisibleDcc.SetValueAndForceNotify(!_isVisibleDcc.Value);
            });

            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();
            _battlePreparation = Find<BattlePreparation>();
            _arenaPreparation = Find<ArenaBattlePreparation>();
            _raidPreparation = Find<RaidPreparation>();
            _grind = Find<Grind>();

            information.Initialize(true, UpdateNotification);

            grindModeToggle.onValueChanged.AddListener(toggledOn =>
            {
                grindModePanel.SetActive(toggledOn);
                statusObject.SetActive(!toggledOn);
                equipmentSlotObject.SetActive(!toggledOn);
                if (toggledOn)
                {
                    grindModule.Show();
                }
                else
                {
                    grindModule.gameObject.SetActive(false);
                    information.UpdateInventory(BattleType.Adventure);
                }
            });

            grindModeToggle.onClickObsoletedToggle.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_STAGE_LOCK_FORMAT",
                        Game.LiveAsset.GameConfig.RequiredStage.Grind),
                    NotificationCell.NotificationType.UnlockCondition);
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var clearedStageId = States.Instance.CurrentAvatarState
                .worldInformation.TryGetLastClearedStageId(out var stageId) ? stageId : 1;
            grindModeToggle.obsolete = clearedStageId < Game.LiveAsset.GameConfig.RequiredStage.Grind;
            grindModeToggle.isOn = false;
            information.UpdateInventory(BattleType.Adventure);
            OnClickPresetTab(adventureButton, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);

            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var isActiveDcc = Dcc.instance.IsVisible(avatarState.address, out var id, out var isVisible);
            _isVisibleDcc.Value = isActiveDcc;
            _isVisibleDcc.Subscribe(x =>
            {
                _activeDcc.sprite = x ? activeSprite : disableSprite;
                _activeCostume.sprite = x ? disableSprite : activeSprite;
                information.UpdateView(_currentBattleType);
            }).AddTo(gameObject);

            activeDccButton.gameObject.SetActive(isActiveDcc);
            activeCostumeButton.gameObject.SetActive(isActiveDcc);
            dccImage.sprite = isActiveDcc ? SpriteHelper.GetDccProfileIcon(id) : dccSlotDefaultSprite;
            if (isActiveDcc)
            {
                _isVisibleDcc.SetValueAndForceNotify(isVisible);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            if (_battlePreparation.isActiveAndEnabled)
            {
                _battlePreparation.UpdateInventory();
            }

            if (_arenaPreparation.isActiveAndEnabled)
            {
                _arenaPreparation.UpdateInventory();
            }

            if (_raidPreparation.isActiveAndEnabled)
            {
                _raidPreparation.UpdateInventory();
            }

            if (_grind.isActiveAndEnabled)
            {
                _grind.UpdateInventory();
            }
        }

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType,
            System.Action onSetToggle = null)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            onSetToggle?.Invoke();
            _currentBattleType = battleType;

            UpdateNickname(currentAvatarState.level, currentAvatarState.NameWithHash);
            information.UpdateView(battleType);
            AudioController.PlayClick();
        }

        private void UpdateNickname(int level, string nameWithHash)
        {
            nicknameText.text = string.Format(NicknameTextFormat, level, nameWithHash);
        }

        private void UpdateNotification()
        {
            adventureButton.HasNotification.Value = false;
            arenaButton.HasNotification.Value = false;
            raidButton.HasNotification.Value = false;

            var clearedStageId = States.Instance.CurrentAvatarState
                .worldInformation.TryGetLastClearedStageId(out var id) ? id : 1;
            var adventure = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var arena = States.Instance.CurrentItemSlotStates[BattleType.Arena];
            var raid = States.Instance.CurrentItemSlotStates[BattleType.Raid];
            var bestEquipments = information.GetBestEquipments();
            foreach (var guid in bestEquipments)
            {
                if (!adventure.Equipments.Exists(x => x == guid))
                {
                    adventureButton.HasNotification.Value = true;
                }

                if (!arena.Equipments.Exists(x => x == guid))
                {
                    arenaButton.HasNotification.Value =
                        clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena;
                }

                if (!raid.Equipments.Exists(x => x == guid))
                {
                    raidButton.HasNotification.Value =
                        clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.WorldBoss;
                }
            }

            for (var i = 1; i < (int)BattleType.End; i++)
            {
                var battleType = (BattleType)i;
                var inventoryItems = information.GetBestRunes(battleType);
                foreach (var inventoryItem in inventoryItems)
                {
                    var slots = States.Instance.CurrentRuneSlotStates[battleType].GetRuneSlot();
                    if (!slots.Exists(x => x.RuneId == inventoryItem.RuneState.RuneId))
                    {
                        switch (battleType)
                        {
                            case BattleType.Adventure:
                                adventureButton.HasNotification.Value = true;
                                break;
                            case BattleType.Arena:
                                arenaButton.HasNotification.Value =
                                    clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena;
                                break;
                            case BattleType.Raid:
                                raidButton.HasNotification.Value =
                                    clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.WorldBoss;
                                break;
                        }
                    }
                }
            }
        }

        #region For tutorial5

        public void TutorialActionClickAvatarInfoFirstInventoryCellView()
        {
            if (information.TryGetFirstCell(out var item))
            {
                item.Selected.Value = true;
            }
            else
            {
                NcDebug.LogError($"TutorialActionClickAvatarInfoFirstInventoryCellView() throw error.");
            }
        }

        public void TutorialActionCloseAvatarInfoWidget() => Close();

        #endregion
    }
}
