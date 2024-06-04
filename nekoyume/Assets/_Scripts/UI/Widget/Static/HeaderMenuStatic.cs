using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Battle;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Game;
    using Game.Controller;
    using Helper;
    using WorldBoss;
    using Scroller;
    using System.Globalization;
    using UniRx;

    public class HeaderMenuStatic : StaticWidget
    {
        private const int MaxShowMaterialCount = 3;

        public enum ToggleType
        {
            Quest,
            AvatarInfo,
            CombinationSlots,
            Mail,
            Rank,
            Chat,
            Settings,
            Quit,
            PortalReward,
            Notice,
            InviteFriend,
        }

        public enum AssetVisibleState
        {
            Main = 0,
            Combination,
            Shop,
            Battle,
            Arena,
            EventDungeon,
            WorldBoss,
            CurrencyOnly,
            RuneStone,
            Mileage,
            Summon,
        }

        [Serializable]
        private class ToggleInfo
        {
            public ToggleType Type;
            public Toggle Toggle;
            public List<Image> Notification;
            public GameObject Lock;
            public TextMeshProUGUI LockText;
        }

        [SerializeField]
        private List<ToggleInfo> toggles = new List<ToggleInfo>();

        [SerializeField]
        private Gold ncg;

        [SerializeField]
        private ActionPoint actionPoint;

        [SerializeField]
        private Crystal crystal;

        [SerializeField]
        private Hourglass hourglass;

        [SerializeField]
        private RuneStone runeStone;

        [SerializeField]
        private ArenaTickets arenaTickets;

        [SerializeField]
        private EventDungeonTickets eventDungeonTickets;

        [SerializeField]
        private WorldBossTickets worldBossTickets;

        [SerializeField]
        private MaterialAsset[] materialAssets;

        [SerializeField]
        private GameObject mileage;

        [SerializeField]
        private VFX inventoryVFX;

        [SerializeField]
        private VFX workshopVFX;

        [SerializeField]
        private Toggle menuToggleDropdown;

        [SerializeField]
        private List<Image> menuToggleNotifications;

        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        private readonly Dictionary<ToggleType, Widget> _toggleWidgets =
            new Dictionary<ToggleType, Widget>();

        private readonly Dictionary<ToggleType, ReactiveProperty<bool>> _toggleNotifications =
            new Dictionary<ToggleType, ReactiveProperty<bool>>()
            {
                { ToggleType.Quest, new ReactiveProperty<bool>(false) },
                { ToggleType.AvatarInfo, new ReactiveProperty<bool>(false) },
                { ToggleType.CombinationSlots, new ReactiveProperty<bool>(false) },
                { ToggleType.Mail, new ReactiveProperty<bool>(false) },
                { ToggleType.Rank, new ReactiveProperty<bool>(false) },
                { ToggleType.PortalReward, new ReactiveProperty<bool>(false) },
                { ToggleType.Notice, new ReactiveProperty<bool>(false) },
                { ToggleType.InviteFriend, new ReactiveProperty<bool>(false) },
            };

        private readonly Dictionary<ToggleType, int> _toggleUnlockStages =
            new Dictionary<ToggleType, int>()
            {
                { ToggleType.Quest, 1 },
                { ToggleType.AvatarInfo, 1 },
                { ToggleType.CombinationSlots, 0 },
                { ToggleType.Mail, 1 },
                { ToggleType.Rank, 1 },
                { ToggleType.Chat, 1 },
                { ToggleType.Settings, 1 },
                { ToggleType.Quit, 1 },
            };

        private long _blockIndex;

        public Gold Gold => ncg;
        public ActionPoint ActionPoint => actionPoint;
        public Crystal Crystal => crystal;
        public Hourglass Hourglass => hourglass;
        public RuneStone RuneStone => runeStone;
        public ArenaTickets ArenaTickets => arenaTickets;
        public EventDungeonTickets EventDungeonTickets => eventDungeonTickets;
        public WorldBossTickets WorldBossTickets => worldBossTickets;
        public MaterialAsset[] MaterialAssets => materialAssets;

        public override bool CanHandleInputEvent => false;

        private const string PortalRewardNotificationKey = "PORTAL_REWARD_NOTIFICATION";

        public const string PortalRewardNotificationCombineKey = "PORTAL_REWARD_NOTIFICATION_COMBINE_ACTION";
        public const string PortalRewardNotificationTradingKey = "PORTAL_REWARD_NOTIFICATION_TRADING_ACTION";
        public const string PortalRewardNotificationDailyKey = "PORTAL_REWARD_NOTIFICATION_DAILY_ACTION";

        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public override void Initialize()
        {
            base.Initialize();

            _toggleWidgets.Add(ToggleType.Quest, Find<QuestPopup>());
            _toggleWidgets.Add(ToggleType.AvatarInfo, Find<AvatarInfoPopup>());
            _toggleWidgets.Add(ToggleType.CombinationSlots, Find<CombinationSlotsPopup>());
            _toggleWidgets.Add(ToggleType.Mail, Find<MailPopup>());
            _toggleWidgets.Add(ToggleType.Rank, Find<RankPopup>());
            _toggleWidgets.Add(ToggleType.Settings, Find<SettingPopup>());
            _toggleWidgets.Add(ToggleType.Chat, Find<ChatPopup>());

            foreach (var toggleInfo in toggles)
            {
                if (_toggleNotifications.ContainsKey(toggleInfo.Type))
                {
                    foreach (var notiObj in toggleInfo.Notification)
                    {
                        _toggleNotifications[toggleInfo.Type].SubscribeTo(notiObj)
                            .AddTo(gameObject);
                    }
                }

                switch (toggleInfo.Type)
                {
                    case ToggleType.InviteFriend:
                        toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                        {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
                            var widget = Find<InviteFriendsPopup>();
                            if (value)
                            {
                                var stage = Game.instance.Stage;
                                if (!BattleRenderer.Instance.IsOnBattle || stage.SelectedPlayer.IsAlive)
                                {
                                    widget.Show(() => { toggleInfo.Toggle.isOn = false; });
                                }
                            }
                            else
                            {
                                if (widget.isActiveAndEnabled)
                                {
                                    widget.Close(true);
                                }
                            }
#else
                            Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE",
                                "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
                            toggleInfo.Toggle.isOn = false;
#endif
                        });
                        break;
                    case ToggleType.PortalReward:
                        if (Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
                        {
                            toggleInfo.Toggle.gameObject.SetActive(false);
                        }
                        else
                        {
                            toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                            {
                                var confirm = Find<TitleOneButtonSystem>();
                                if (value)
                                {
                                    var stage = Game.instance.Stage;
                                    if (!BattleRenderer.Instance.IsOnBattle || stage.SelectedPlayer.IsAlive)
                                    {
                                        confirm.SubmitCallback = () =>
                                        {
                                            Game.instance.PortalConnect.OpenPortalRewardUrl();
                                            confirm.Close();
                                        };
                                        confirm.Set("UI_INFORMATION_PORTAL_REWARD",
                                            "UI_DESCRIPTION_PORTAL_REWARD", true);
                                        confirm.Show(() => toggleInfo.Toggle.isOn = false);
                                    }
                                }
                                else
                                {
                                    if (confirm.isActiveAndEnabled)
                                    {
                                        confirm.Close(true);
                                    }
                                }

                                UpdatePortalReward(false);
                            });
                        }

                        break;
                    case ToggleType.Quit:
                        toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                        {
                            var confirm = Find<TitleOneButtonSystem>();
                            if (value)
                            {
                                var stage = Game.instance.Stage;
                                if (!BattleRenderer.Instance.IsOnBattle || stage.SelectedPlayer.IsAlive)
                                {
                                    confirm.SubmitCallback = () =>
                                    {
                                        var address = States.Instance.CurrentAvatarState.address;
                                        if (WorldBossStates.IsReceivingGradeRewards(address))
                                        {
                                            OneLineSystem.Push(
                                                MailType.System,
                                                L10nManager.Localize("UI_CAN_NOT_CHANGE_CHARACTER"),
                                                NotificationCell.NotificationType.Alert);
                                            return;
                                        }

                                        Game.instance.BackToNest();
                                        Close();
                                        AudioController.PlayClick();
                                    };
                                    confirm.Set("UI_INFORMATION_CHARACTER_SELECT", "UI_DESCRIPTION_CHARACTER_SELECT", true);
                                    confirm.Show(() => { toggleInfo.Toggle.isOn = false; });
                                }
                            }
                            else
                            {
                                if (confirm.isActiveAndEnabled)
                                {
                                    confirm.Close(true);
                                }
                            }
                        });
                        break;
                    case ToggleType.Notice:
                        toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                        {
                            var widget = Find<EventReleaseNotePopup>();
                            if (value)
                            {
                                var stage = Game.instance.Stage;
                                if (!BattleRenderer.Instance.IsOnBattle || stage.SelectedPlayer.IsAlive)
                                {
                                    widget.ShowNotFiltered(() => { toggleInfo.Toggle.isOn = false; });
                                }
                            }
                            else
                            {
                                if (widget.isActiveAndEnabled)
                                {
                                    widget.Close(true);
                                }
                            }
                        });
                        break;
                    default:
                        toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                        {
                            var widget = _toggleWidgets[toggleInfo.Type];
                            if (value)
                            {
                                if (_toggleUnlockStages.TryGetValue(toggleInfo.Type, out var requiredStage) &&
                                    requiredStage != 0 &&
                                    !States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
                                {
                                    OneLineSystem.Push(MailType.System,
                                        L10nManager.Localize("UI_STAGE_LOCK_FORMAT", requiredStage),
                                        NotificationCell.NotificationType.UnlockCondition);
                                    toggleInfo.Toggle.isOn = false;
                                    return;
                                }

                                var stage = Game.instance.Stage;
                                if (!BattleRenderer.Instance.IsOnBattle || stage.SelectedPlayer.IsAlive)
                                {
                                    widget.Show(() => { toggleInfo.Toggle.isOn = false; });
                                }
                            }
                            else
                            {
                                if (widget.isActiveAndEnabled)
                                {
                                    widget.Close(true);
                                }
                            }
                        });
                        break;
                }
            }

            menuToggleDropdown.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    CloseWidget = () => { menuToggleDropdown.isOn = false; };
                    WidgetStack.Push(gameObject);
                    Animator.Play("HamburgerMenu@Show");
                }
                else
                {
                    Animator.Play("HamburgerMenu@Close",-1,1);
                    CloseWidget = null;
                    Observable.NextFrame().Subscribe(_ =>
                    {
                        var list = WidgetStack.ToList();
                        list.Remove(gameObject);
                        WidgetStack.Clear();
                        foreach (var go in list)
                        {
                            WidgetStack.Push(go);
                        }
                    });
                }

                foreach (var toggleInfo in toggles)
                {
                    if (!value || !toggleInfo.Lock || !toggleInfo.LockText)
                    {
                        continue;
                    }

                    var requiredStage = _toggleUnlockStages[toggleInfo.Type];
                    var isLock = requiredStage != 0 &&
                                 !States.Instance.CurrentAvatarState.worldInformation
                                     .IsStageCleared(requiredStage);
                    toggleInfo.Lock.SetActive(isLock);
                    toggleInfo.LockText.text = L10nManager.Localize("UI_STAGE") + requiredStage;
                }
            });

            Event.OnRoomEnter.AddListener(_ => UpdateAssets(AssetVisibleState.Main));
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);

            _toggleNotifications[ToggleType.Notice].Value = LiveAssetManager.instance.HasUnread;
            LiveAssetManager.instance.ObservableHasUnread
                .SubscribeTo(_toggleNotifications[ToggleType.Notice])
                .AddTo(gameObject);

            IObservable<IList<bool>> mergedMenuNoti;
            if (!Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                mergedMenuNoti = Observable.CombineLatest(
                    _toggleNotifications[ToggleType.Notice],
                    _toggleNotifications[ToggleType.PortalReward],
                    _toggleNotifications[ToggleType.Rank]);
            }
            else
            {
                mergedMenuNoti = Observable.CombineLatest(
                    _toggleNotifications[ToggleType.Notice],
                    _toggleNotifications[ToggleType.Rank]);
            }

            foreach (var item in menuToggleNotifications)
            {
                mergedMenuNoti.Subscribe(notices => item.enabled = notices.Any(noti => noti))
                    .AddTo(gameObject);
            }

            _toggleNotifications[ToggleType.PortalReward].Value =
                PlayerPrefs.GetInt(PortalRewardNotificationKey, 0) != 0;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposablesAtOnEnable.DisposeAllAndClear();
            ReactiveAvatarState.QuestList?.Subscribe(SubscribeAvatarQuestList)
                .AddTo(_disposablesAtOnEnable);
            LocalMailHelper.Instance.ObservableMailBox.Subscribe(SubscribeAvatarMailBox)
                .AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.Inventory?.Subscribe(SubscribeInventory)
                .AddTo(_disposablesAtOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public void Show(AssetVisibleState assetVisibleState, bool ignoreShowAnimation = false)
        {
            UpdateAssets(assetVisibleState);
            Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            foreach (var toggleInfo in toggles)
            {
                toggleInfo.Toggle.isOn = false;
            }

            menuToggleDropdown.isOn = false;
            base.Close(ignoreCloseAnimation);
        }

        public void PlayVFX(ItemMoveAnimation.EndPoint endPoint)
        {
            switch (endPoint)
            {
                case ItemMoveAnimation.EndPoint.Inventory:
                    inventoryVFX.Play();
                    break;
                case ItemMoveAnimation.EndPoint.Workshop:
                    workshopVFX.Play();
                    break;
            }
        }

        public Transform GetToggle(ToggleType toggleType)
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(toggleType));
            var toggleTransform = info?.Toggle.transform;
            return toggleTransform ? toggleTransform : null;
        }

        public void UpdateAssets(AssetVisibleState state)
        {
            switch (state)
            {
                case AssetVisibleState.Main:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isActionPointActive: true);
                    break;
                case AssetVisibleState.Combination:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isHourglassActive: true);
                    break;
                case AssetVisibleState.Shop:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isMaterialActiveCount: 1); // isMaterialActiveCount : Golden dust
                    SetMaterial(0, CostType.GoldDust);
                    break;
                case AssetVisibleState.Battle:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isActionPointActive: true);
                    break;
                case AssetVisibleState.Arena:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isArenaTicketsActive: true);
                    break;
                case AssetVisibleState.EventDungeon:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isEventDungeonTicketsActive: true);
                    break;
                case AssetVisibleState.WorldBoss:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isEventWorldBossTicketsActive: true);
                    break;
                case AssetVisibleState.CurrencyOnly:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true);
                    break;
                case AssetVisibleState.RuneStone:
                    SetActiveAssets(isNcgActive: true, isCrystalActive: true, isRuneStoneActive: true);
                    break;
                case AssetVisibleState.Mileage:
                    SetActiveAssets(isNcgActive: true, isCrystalActive:true, isMileageActive: true);
                    break;
                case AssetVisibleState.Summon:
                    SetActiveAssets(isNcgActive: true, isMaterialActiveCount: 3);
                    break;
            }
        }

        public void SetMaterial(int index, CostType costType)
        {
            var icon = costIconData.GetIcon(costType);
            var count = States.Instance.CurrentAvatarState.inventory
                .GetMaterialCount((int)costType);

            MaterialAssets[index].SetMaterial(icon, count, costType);
        }

        // TODO: 정확한 상황을 알지 못하지만, SetMaterial 메서드 호출 이후 자동으로 호출되야 할 것같이 생김.
        private void SetActiveAssets(
            bool isNcgActive = false,
            bool isCrystalActive = false,
            bool isActionPointActive = false,
            bool isHourglassActive = false,
            bool isArenaTicketsActive = false,
            bool isEventDungeonTicketsActive = false,
            bool isEventWorldBossTicketsActive = false,
            bool isRuneStoneActive = false,
            bool isMileageActive = false,
            int isMaterialActiveCount = 0)
        {
            crystal.gameObject.SetActive(isCrystalActive);
            actionPoint.gameObject.SetActive(isActionPointActive);
            hourglass.gameObject.SetActive(isHourglassActive);
            arenaTickets.gameObject.SetActive(isArenaTicketsActive);
            eventDungeonTickets.gameObject.SetActive(isEventDungeonTicketsActive);
            worldBossTickets.gameObject.SetActive(isEventWorldBossTicketsActive);
            runeStone.gameObject.SetActive(isRuneStoneActive);
            mileage.gameObject.SetActive(isMileageActive);
            for (var i = 0; i < materialAssets.Length; i++)
            {
                materialAssets[i].gameObject.SetActive(i < isMaterialActiveCount);
            }

            ncg.gameObject.SetActive(isMaterialActiveCount < MaxShowMaterialCount && isNcgActive);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateCombinationNotification(blockIndex);

            var mailBox = Find<MailPopup>().MailBox;
            if (mailBox is null)
            {
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= blockIndex);
        }

        private void SubscribeAvatarMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                NcDebug.LogWarning($"{nameof(mailBox)} is null.");
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                NcDebug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            var hasNotification =
                questList.Any(quest => quest.IsPaidInAction && quest.isReceivable);
            _toggleNotifications[ToggleType.Quest].Value = hasNotification;
            Find<QuestPopup>().SetList(questList);
        }

        private void SubscribeInventory(Nekoyume.Model.Item.Inventory inventory)
        {
            var blockIndex = Game.instance.Agent.BlockIndex;
            var avatarLevel = States.Instance.CurrentAvatarState?.level ?? 0;
            var sheets = Game.instance.TableSheets;
            var hasNotification = inventory?.HasNotification(avatarLevel, blockIndex,
                sheets.ItemRequirementSheet,
                sheets.EquipmentItemRecipeSheet,
                sheets.EquipmentItemSubRecipeSheetV2,
                sheets.EquipmentItemOptionSheet) ?? false;
            UpdateInventoryNotification(hasNotification);
        }

        private void UpdateCombinationNotification(long currentBlockIndex)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            var states = States.Instance.GetCombinationSlotState(avatarState, currentBlockIndex);
            var hasNotification = states?.Any(state =>
                HasCombinationNotification(state.Value, currentBlockIndex)) ?? false;
            _toggleNotifications[ToggleType.CombinationSlots].Value = hasNotification;
        }

        private bool HasCombinationNotification(CombinationSlotState state, long currentBlockIndex)
        {
            if (state?.Result is null)
            {
                return false;
            }

            var isAppraise = currentBlockIndex < state.StartBlockIndex +
                States.Instance.GameConfigState.RequiredAppraiseBlock;
            if (isAppraise)
            {
                return false;
            }

            var gameConfigState = Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            }
            var row = Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            var isEnough =
                States.Instance.CurrentAvatarState.inventory.HasFungibleItem(row.ItemId, currentBlockIndex, cost);
            return isEnough;
        }

        public void UpdateInventoryNotification(bool hasNotification)
        {
            _toggleNotifications[ToggleType.AvatarInfo].Value = hasNotification;
        }

        public void UpdateMailNotification(bool hasNotification)
        {
            _toggleNotifications[ToggleType.Mail].Value = hasNotification;
        }

        public void UpdatePortalReward(bool hasNotification)
        {
            _toggleNotifications[ToggleType.PortalReward].Value = hasNotification;
            PlayerPrefs.SetInt(PortalRewardNotificationKey, hasNotification ? 1:0);
        }

        public void SetActiveAvatarInfo(bool value)
        {
            var avatarInfo = toggles.FirstOrDefault(x => x.Type == ToggleType.AvatarInfo);
            avatarInfo?.Toggle.gameObject.SetActive(value);
        }

        public void TutorialActionClickBottomMenuWorkShopButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.CombinationSlots));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuMailButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.Mail));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuCharacterButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.AvatarInfo));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickMenuButton()
        {
            if(menuToggleDropdown != null)
            {
                menuToggleDropdown.isOn = true;
            }
        }

        public void TutorialActionClickPortalRewardButton()
        {
            Game.instance.PortalConnect.OpenPortalRewardUrl();
            if (menuToggleDropdown != null)
            {
                menuToggleDropdown.isOn = false;
            }
            UpdatePortalReward(false);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionActionPointHeaderMenu()
        {
            actionPoint.ShowMaterialNavigationPopup();
        }

        public void UpdatePortalRewardByLevel(int level)
        {
            foreach (var noticePoint in ResourcesHelper.GetPortalRewardLevelTable())
            {
                if (noticePoint == level)
                {
                    UpdatePortalReward(true);
                    return;
                }
            }
        }

        public void UpdatePortalRewardOnce(string key)
        {
            var count = PlayerPrefs.GetInt(key, 0);
            if(count == 0) {
                UpdatePortalReward(true);
            }
            PlayerPrefs.SetInt(key, ++count);
        }

        public void UpdatePortalRewardDaily()
        {
            var updateAtToday = true;
            if (PlayerPrefs.HasKey(PortalRewardNotificationDailyKey) &&
                DateTime.TryParseExact(PlayerPrefs.GetString(PortalRewardNotificationDailyKey),
                    DateTimeFormat,
                    null,
                    DateTimeStyles.None,
                    out var result))
            {
                updateAtToday = DateTime.Today != result.Date;
            }

            if (updateAtToday)
            {
                UpdatePortalReward(true);
                PlayerPrefs.SetString(PortalRewardNotificationDailyKey, DateTime.Today.ToString(DateTimeFormat));
            }
        }
    }
}
