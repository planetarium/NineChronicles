using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
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
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class HeaderMenuStatic : StaticWidget
    {
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
        }

        public enum AssetVisibleState
        {
            Main = 0,
            Combination,
            Shop,
            Battle,
            Arena,
        }

        [Serializable]
        private class ToggleInfo
        {
            public ToggleType Type;
            public Toggle Toggle;
            public Image Notification;
            public GameObject Lock;
            public TextMeshProUGUI LockText;
        }

        [SerializeField] private List<ToggleInfo> toggles = new List<ToggleInfo>();
        [SerializeField] private GameObject ncg;
        [SerializeField] private ActionPoint actionPoint;
        [SerializeField] private Crystal crystal;
        [SerializeField] private GameObject dailyBonus;
        [SerializeField] private GameObject hourglass;
        [SerializeField] private GameObject arenaTickets;
        [SerializeField] private VFX inventoryVFX;
        [SerializeField] private VFX workshopVFX;
        [SerializeField] private Image actionPointImage;
        [SerializeField] private ToggleDropdown menuToggleDropdown;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        private readonly Dictionary<ToggleType, Widget> _toggleWidgets =
            new Dictionary<ToggleType, Widget>();

        private readonly Dictionary<ToggleType, ReactiveProperty<bool>> _toggleNotifications =
            new Dictionary<ToggleType, ReactiveProperty<bool>>()
            {
                {ToggleType.Quest, new ReactiveProperty<bool>(false)},
                {ToggleType.AvatarInfo, new ReactiveProperty<bool>(false)},
                {ToggleType.CombinationSlots, new ReactiveProperty<bool>(false)},
                {ToggleType.Mail, new ReactiveProperty<bool>(false)},
                {ToggleType.Rank, new ReactiveProperty<bool>(false)},
            };

        private readonly Dictionary<ToggleType, int> _toggleUnlockStages =
            new Dictionary<ToggleType, int>()
            {
                {ToggleType.Quest, GameConfig.RequireClearedStageLevel.UIBottomMenuQuest},
                {ToggleType.AvatarInfo, GameConfig.RequireClearedStageLevel.UIBottomMenuCharacter},
                {ToggleType.CombinationSlots, GameConfig.RequireClearedStageLevel.CombinationEquipmentAction},
                {ToggleType.Mail, GameConfig.RequireClearedStageLevel.UIBottomMenuMail},
                {ToggleType.Rank, 1},
                {ToggleType.Chat, GameConfig.RequireClearedStageLevel.UIBottomMenuChat},
                {ToggleType.Settings, 1},
                {ToggleType.Quit, 1},
            };

        private long _blockIndex;

        public Image ActionPointImage => actionPointImage;

        public bool ChargingAP => actionPoint.NowCharging;

        public Crystal Crystal => crystal;

        public override bool CanHandleInputEvent => false;

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
            _toggleWidgets.Add(ToggleType.Quit, Find<QuitSystem>());

            foreach (var toggleInfo in toggles)
            {
                if (_toggleNotifications.ContainsKey(toggleInfo.Type))
                {
                    _toggleNotifications[toggleInfo.Type].SubscribeTo(toggleInfo.Notification)
                        .AddTo(gameObject);
                }

                toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                {
                    var widget = _toggleWidgets[toggleInfo.Type];
                    if (value)
                    {
                        var requiredStage = _toggleUnlockStages[toggleInfo.Type];
                        if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
                        {
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("UI_STAGE_LOCK_FORMAT", requiredStage),
                                NotificationCell.NotificationType.UnlockCondition);
                            toggleInfo.Toggle.isOn = false;
                            return;
                        }

                        var stage = Game.Game.instance.Stage;
                        if (!stage.IsInStage || stage.SelectedPlayer.IsAlive)
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
            }

            menuToggleDropdown.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    CloseWidget = () => { menuToggleDropdown.isOn = false; };
                    WidgetStack.Push(gameObject);
                }
                else
                {
                    CloseWidget = null;
                    Observable.NextFrame().Subscribe(_ =>
                    {
                        var list = WidgetStack.ToList();
                        list.Remove(gameObject);
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
                    var isLock = !States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage);
                    toggleInfo.Lock.SetActive(isLock);
                    toggleInfo.LockText.text = L10nManager.Localize("UI_STAGE") + requiredStage;
                }
            });

            Game.Event.OnRoomEnter.AddListener(_ => UpdateAssets(AssetVisibleState.Main));
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposablesAtOnEnable.DisposeAllAndClear();
            ReactiveAvatarState.QuestList?.Subscribe(SubscribeAvatarQuestList)
                .AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.MailBox?.Subscribe(SubscribeAvatarMailBox)
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
                    SetActiveAssets(isNcgActive: true, isActionPointActive: true, isDailyBonusActive: true);
                    break;
                case AssetVisibleState.Combination:
                    SetActiveAssets(isNcgActive: true, isActionPointActive: true, isHourglassActive: true);
                    break;
                case AssetVisibleState.Shop:
                case AssetVisibleState.Battle:
                    SetActiveAssets(isNcgActive: true, isActionPointActive: true);
                    break;
                case AssetVisibleState.Arena:
                    SetActiveAssets(isNcgActive: true, isActionPointActive: true, isArenaTicketsActive: true);
                    break;
            }
        }

        private void SetActiveAssets(
            bool isNcgActive = false,
            bool isActionPointActive = false,
            bool isDailyBonusActive = false,
            bool isHourglassActive = false,
            bool isArenaTicketsActive = false)
        {
            ncg.SetActive(isNcgActive);
            actionPoint.gameObject.SetActive(isActionPointActive);
            dailyBonus.SetActive(isDailyBonusActive);
            hourglass.SetActive(isHourglassActive);
            arenaTickets.SetActive(isArenaTicketsActive);
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
                Debug.LogWarning($"{nameof(mailBox)} is null.");
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                Debug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            var hasNotification =
                questList.Any(quest => quest.IsPaidInAction && quest.isReceivable);
            _toggleNotifications[ToggleType.Quest].Value = hasNotification;
            Find<QuestPopup>().SetList(questList);
        }

        private void SubscribeInventory(Nekoyume.Model.Item.Inventory inventory)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var avatarLevel = States.Instance.CurrentAvatarState?.level ?? 0;
            var hasNotification = inventory?.HasNotification(avatarLevel, blockIndex) ?? false;
            UpdateInventoryNotification(hasNotification);
        }

        private void UpdateCombinationNotification(long currentBlockIndex)
        {
            var states = States.Instance.GetCombinationSlotState(currentBlockIndex);
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

            var isAppraise = currentBlockIndex < state.StartBlockIndex + GameConfig.RequiredAppraiseBlock;
            if (isAppraise)
            {
                return false;
            }

            var gameConfigState = Game.Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            var isEnough =  States.Instance.CurrentAvatarState.inventory.HasFungibleItem(row.ItemId, currentBlockIndex, cost);
            return isEnough;
        }

        public void UpdateInventoryNotification(bool hasNotification)
        {
            _toggleNotifications[ToggleType.AvatarInfo].Value = hasNotification;
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
    }
}
