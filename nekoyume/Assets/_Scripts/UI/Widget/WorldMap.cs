using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Model;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using UnityEngine;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Scroller;
using TMPro;
using Unity.Mathematics;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using mixpanel;
    using UniRx;

    public class WorldMap : Widget
    {
        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new(false);
            public readonly ReactiveProperty<int> SelectedWorldId = new(1);
            public readonly ReactiveProperty<int> SelectedStageId = new(1);

            public WorldInformation WorldInformation;
            public List<int> UnlockedWorldIds;
        }

        [Serializable]
        public class EventDungeonObject
        {
            public int eventId;
            public WorldButton button;
            public GameObject remainingTimeObject;
            public TextMeshProUGUI remainingTimeText;
        }

        [SerializeField]
        private GameObject worldMapRoot;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private WorldButton[] _worldButtons;

        [SerializeField]
        private EventDungeonObject[] eventDungeonObjects;

        [SerializeField]
        private Button eventDungeonLockButton;

        private readonly List<IDisposable> _disposablesAtShow = new();

        public ViewModel SharedViewModel { get; private set; }

        public bool HasNotification { get; private set; }

        public int StageIdToNotify { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = TableSheets.Instance.StageWaveSheet.First?.StageId ?? 1;
            SharedViewModel = new ViewModel
            {
                SelectedStageId =
                {
                    Value = firstStageId
                }
            };
            var worldSheet = TableSheets.Instance.WorldSheet;
            foreach (var worldButton in _worldButtons)
            {
                if (!worldSheet.TryGetByName(worldButton.WorldName, out var row))
                {
                    worldButton.Hide();
                    continue;
                }

                worldButton.Set(row);
                worldButton.Show();
                worldButton.OnClickSubject
                    .Subscribe(button =>
                    {
                        if (button.IsUnlockable)
                        {
                            if (!ShowManyWorldUnlockPopup(SharedViewModel.WorldInformation))
                            {
                                ShowWorldUnlockPopup(button.Id);
                            }
                        }
                        else
                        {
                            ShowWorld(button.Id);
                        }
                    }).AddTo(gameObject);
            }

            foreach (var eventDungeonButton in eventDungeonObjects.Select(i => i.button))
            {
                eventDungeonButton.Lock();
                eventDungeonButton.Hide();
                eventDungeonButton.OnClickSubject.Subscribe(_ =>
                {
                    if (RxProps.EventScheduleRowForDungeon.Value is null)
                    {
                        return;
                    }

                    ShowEventDungeonStage(RxProps.EventDungeonRow, false);
                }).AddTo(gameObject);
            }

            eventDungeonLockButton.onClick.AddListener(() =>
            {
                if (RxProps.EventScheduleRowForDungeon.Value is null)
                {
                    NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                        NotificationCell.NotificationType.Information);
                }
            });
        }

        #endregion

        public void Show(WorldInformation worldInformation, bool blockWorldUnlockPopup = false)
        {
            SubscribeAtShow();

            HasNotification = false;
            SetWorldInformation(worldInformation);

            var status = Find<Status>();
            status.Close(true);
            Show(true);

            if (!blockWorldUnlockPopup)
            {
                ShowManyWorldUnlockPopup(worldInformation);
            }
        }

        public void Show(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            SubscribeAtShow();
            ShowWorld(worldId, stageId, showWorld, callByShow);
            Show(true);
        }

        private void SubscribeAtShow()
        {
            _disposablesAtShow.DisposeAllAndClear();
            OnDisableStaticObservable
                .Where(widget => widget is StageInformation)
                .DelayFrame(1)
                .Where(_ => gameObject.activeSelf)
                .Subscribe(_ => SubscribeAtShow())
                .AddTo(_disposablesAtShow);
            TextMeshProUGUI eventDungeonRemainingTimeText = null;
            RxProps.EventScheduleRowForDungeon.Subscribe(value =>
            {
                foreach (var eventDungeonObject in eventDungeonObjects)
                {
                    eventDungeonObject.button.Hide();
                    eventDungeonObject.remainingTimeObject.SetActive(false);
                }

                if (value is null)
                {
                    Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                    eventDungeonLockButton.gameObject.SetActive(true);
                }
                else
                {
                    Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);

                    eventDungeonLockButton.gameObject.SetActive(false);
                    var eventDungeonObject = eventDungeonObjects.Last(o => o.eventId == value.Id);
                    eventDungeonObject.button.Show();
                    eventDungeonObject.button.HasNotification.Value = true;
                    eventDungeonObject.button.Unlock();
                    eventDungeonObject.remainingTimeObject.SetActive(true);
                    eventDungeonRemainingTimeText = eventDungeonObject.remainingTimeText;
                }
            }).AddTo(_disposablesAtShow);
            if (eventDungeonRemainingTimeText != null)
            {
                RxProps.EventDungeonRemainingTimeText
                    .SubscribeTo(eventDungeonRemainingTimeText)
                    .AddTo(_disposablesAtShow);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            base.Close(true);
        }

        public void SetWorldInformation(WorldInformation worldInformation)
        {
            SharedViewModel.WorldInformation = worldInformation;
            if (worldInformation is null)
            {
                return;
            }

            foreach (var worldButton in _worldButtons)
            {
                if (!worldButton.IsShown)
                {
                    continue;
                }

                var buttonWorldId = worldButton.Id;
                var unlockRow = TableSheets.Instance.WorldUnlockSheet
                    .OrderedList
                    .FirstOrDefault(row => row.WorldIdToUnlock == buttonWorldId);
                var canTryThisWorld = worldInformation.IsStageCleared(unlockRow?.StageId ?? int.MaxValue);
                var worldIsUnlocked =
                    (worldInformation.TryGetWorld(buttonWorldId, out var worldModel) &&
                     worldModel.IsUnlocked) ||
                    canTryThisWorld;

                UpdateNotificationInfo();

                var isIncludedInQuest = StageIdToNotify >= worldButton.StageBegin &&
                                        StageIdToNotify <= worldButton.StageEnd;

                if (worldIsUnlocked)
                {
                    worldButton.HasNotification.Value = isIncludedInQuest;
                    worldButton.Unlock(!SharedViewModel.UnlockedWorldIds.Contains(worldButton.Id));
                }
                else
                {
                    worldButton.Lock();
                }

                SetWorldOpenCostTextColor(States.Instance.CrystalBalance);
            }

            if (!worldInformation.TryGetFirstWorld(out _))
            {
                throw new Exception("worldInformation.TryGetFirstWorld() failed!");
            }
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
            {
                var unlockConditionRow =
                    TableSheets.Instance.WorldUnlockSheet.OrderedList
                        .FirstOrDefault(row =>
                            row.WorldIdToUnlock == worldId);
                if (unlockConditionRow is null ||
                    !SharedViewModel.WorldInformation
                        .IsStageCleared(unlockConditionRow.StageId))
                {
                    throw new ArgumentException(nameof(worldId));
                }

                var worldSheet = TableSheets.Instance.WorldSheet;
                SharedViewModel.WorldInformation.UnlockWorld(worldId, 0, worldSheet);
                SharedViewModel.WorldInformation.TryGetWorld(worldId, out world);
            }

            if (worldId == 1)
            {
                Analyzer.Instance.Track("Unity/Click Yggdrasil", new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                });

                var evt = new AirbridgeEvent("Click_Yggdrasil");
                evt.AddCustomAttribute("agent-address", States.Instance.AgentState.address.ToString());
                evt.AddCustomAttribute("avatar-address", States.Instance.CurrentAvatarState.address.ToString());
                AirbridgeUnity.TrackEvent(evt);
            }

            Push();
            ShowWorld(world.Id, world.GetNextStageId(), false);
        }

        private void ShowWorld(
            int worldId,
            int stageId,
            bool showWorld,
            bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }
            SubscribeAtShow();

            TableSheets.Instance.WorldSheet.TryGetValue(
                worldId,
                out var worldRow,
                true);
            SharedViewModel.SelectedWorldId.Value = worldId;
            SharedViewModel.SelectedStageId.Value = stageId;
            var stageInfo = Find<StageInformation>();
            stageInfo.Show(SharedViewModel, worldRow, StageType.HackAndSlash);
            UpdateNotificationInfo();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            Find<HeaderMenuStatic>().Show();
        }

        public void ShowEventDungeonStage(
            EventDungeonSheet.Row eventDungeonRow,
            bool showWorld,
            bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }
            SubscribeAtShow();

            Show(true);
            var openedStageId =
                RxProps.EventDungeonInfo.Value is null ||
                RxProps.EventDungeonInfo.Value.ClearedStageId == 0
                    ? RxProps.EventDungeonRow.StageBegin
                    : math.min(
                        RxProps.EventDungeonInfo.Value.ClearedStageId + 1,
                        RxProps.EventDungeonRow.StageEnd);
            SharedViewModel.SelectedWorldId.Value = eventDungeonRow.Id;
            SharedViewModel.SelectedStageId.Value = openedStageId;
            var stageInfo = Find<StageInformation>();
            stageInfo.Show(
                SharedViewModel,
                eventDungeonRow,
                openedStageId,
                openedStageId);
            StageIdToNotify = openedStageId;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);
            Find<HeaderMenuStatic>().Show();
        }

        public void UpdateNotificationInfo()
        {
            var questStageId = Game.Game.instance.States.CurrentAvatarState.questList?
                .OfType<WorldQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.Goal)
                .FirstOrDefault()?
                .Goal ?? -1;
            StageIdToNotify = questStageId;

            HasNotification = questStageId > 0;
        }

        private void CallByShowUpdateWorld()
        {
            var status = Find<Status>();
            status.Close(true);
            worldMapRoot.SetActive(true);
        }

        private void OnAttractInPaymentPopup()
        {
            Close(true);
            Find<Grind>().Show();
        }

        private void ShowWorldUnlockPopup(int worldId)
        {
            var cost = CrystalCalculator.CalculateWorldUnlockCost(
                    new[] { worldId },
                    Game.TableSheets.Instance.WorldUnlockSheet)
                .MajorUnit;
            var balance = States.Instance.CrystalBalance;
            var usageMessage = L10nManager.Localize(
                "UI_UNLOCK_WORLD_FORMAT",
                L10nManager.LocalizeWorldName(worldId));
            Find<PaymentPopup>().Show(
                CostType.Crystal,
                balance.MajorUnit,
                cost,
                balance.GetPaymentFormatText(usageMessage, cost),
                L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                () =>
                {
                    Find<LoadingScreen>().Show(LoadingScreen.LoadingType.WorldUnlock);
                    ActionManager.Instance.UnlockWorld(new List<int> { worldId }, (int) cost).Subscribe();
                },
                OnAttractInPaymentPopup);
        }

        private bool ShowManyWorldUnlockPopup(WorldInformation worldInformation)
        {
            if (worldInformation.TryGetLastClearedStageId(out var stageId))
            {
                var tableSheets = Game.TableSheets.Instance;
                var countOfCanUnlockWorld = Math.Min(stageId / 50,
                    tableSheets.WorldUnlockSheet.Count - 1);
                var worldIdListForUnlock = Enumerable.Range(2, countOfCanUnlockWorld)
                    .Where(i => !SharedViewModel.UnlockedWorldIds.Contains(i))
                    .ToList();

                if (worldIdListForUnlock.Count > 1)
                {
                    var paymentPopup = Find<PaymentPopup>();
                    var cost = CrystalCalculator.CalculateWorldUnlockCost(worldIdListForUnlock,
                        tableSheets.WorldUnlockSheet).MajorUnit;
                    paymentPopup.Show(
                        CostType.Crystal,
                        States.Instance.CrystalBalance.MajorUnit,
                        cost,
                        L10nManager.Localize(
                            "CRYSTAL_MIGRATION_WORLD_ALL_OPEN_FORMAT", cost),
                        L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                        () =>
                        {
                            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.WorldUnlock);
                            ActionManager.Instance.UnlockWorld(worldIdListForUnlock, (int) cost).Subscribe();
                        },
                        OnAttractInPaymentPopup);
                    return true;
                }
            }

            return false;
        }

        private void SetWorldOpenCostTextColor(FungibleAssetValue crystal)
        {
            foreach (var worldButton in _worldButtons)
            {
                worldButton.SetOpenCostTextColor(crystal.MajorUnit);
            }
        }
    }
}
