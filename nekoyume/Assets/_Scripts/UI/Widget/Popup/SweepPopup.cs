using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class SweepPopup : PopupWidget
    {
        [SerializeField]
        private SweepSlider apSlider;

        [SerializeField]
        private SweepSlider apStoneSlider;

        [SerializeField]
        private SweepSlider eventDungeonTicketSlider;

        [SerializeField]
        private ConditionalButton startButton;

        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI starText;

        [SerializeField]
        private TextMeshProUGUI totalApText;

        [SerializeField]
        private TextMeshProUGUI apStoneText;

        [SerializeField]
        private TextMeshProUGUI eventDungeonTicketText;

        [SerializeField]
        private TextMeshProUGUI haveApText;

        [SerializeField]
        private TextMeshProUGUI haveApStoneText;

        [SerializeField]
        private TextMeshProUGUI haveEventDungeonTicketText;

        [SerializeField]
        private TextMeshProUGUI enoughCpText;

        [SerializeField]
        private TextMeshProUGUI insufficientCpText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private GameObject enoughCpContainer;

        [SerializeField]
        private GameObject insufficientCpContainer;

        [SerializeField]
        private GameObject information;

        [SerializeField]
        private GameObject expGlow;

        [SerializeField]
        private Toggle pageToggle;

        [SerializeField]
        private CanvasGroup canvasGroupForRepeat;

        [SerializeField]
        private List<GameObject> objectsForSweep;

        [SerializeField]
        private List<GameObject> objectsForRepeat;

        [SerializeField]
        private List<GameObject> objectsForEventDungeon;

        [SerializeField]
        private GameObject point;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        private TextMeshProUGUI haveText;

        [SerializeField]
        private GameObject potion;

        private readonly ReactiveProperty<int> _apStoneCount = new();
        private readonly ReactiveProperty<int> _ap = new();
        private readonly ReactiveProperty<int> _ticketCount = new();
        private readonly ReactiveProperty<long> _cp = new();
        private readonly List<IDisposable> _disposables = new();

        private StageSheet.Row _stageRow;
        private EventDungeonStageSheet.Row _eventDungeonStageRow;
        private int _worldId;
        private int _costAp;
        private bool _useSweep = true;
        private bool _isEventDungeonMode = false;
        private Action<StageType, int, int, bool> _repeatBattleAction;

        // Event Dungeon specific parameters
        private int _eventScheduleId;
        private int _eventDungeonId;
        private int _eventDungeonStageId;

        private const int UsableApStoneCountWithRepeat = 1;

        private int MaxApStoneCount =>
            _useSweep
                ? HackAndSlashSweep.UsableApStoneCount
                : UsableApStoneCountWithRepeat;

        protected override void Awake()
        {
            _apStoneCount.Subscribe(v => UpdateView()).AddTo(gameObject);
            _ap.Subscribe(v => UpdateView()).AddTo(gameObject);
            _ticketCount.Subscribe(v => UpdateView()).AddTo(gameObject);
            _cp.Subscribe(v => UpdateCpView()).AddTo(gameObject);
            pageToggle.onValueChanged.AddListener(UpdateByToggle);

            startButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    if (_useSweep)
                    {
                        if (_isEventDungeonMode)
                        {
                            EventDungeonSweep(_ticketCount.Value);
                        }
                        else
                        {
                            Sweep(_apStoneCount.Value, _ap.Value, _worldId, _stageRow);
                        }
                    }
                    else
                    {
                        var (count1, count2) = GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value,
                            States.Instance.StakingLevel);
                        _repeatBattleAction(
                            StageType.HackAndSlash,
                            count1 + count2,
                            _apStoneCount.Value,
                            false);
                        Close();
                    }
                })
                .AddTo(gameObject);

            cancelButton.onClick.AddListener(() => Close());

            base.Awake();

            CloseWidget = () =>
            {
                Close();
            };
        }

        public void Show(
            int worldId,
            int stageId,
            Action<StageType, int, int, bool> repeatBattleAction,
            bool ignoreShowAnimation = false)
        {
            if (!TableSheets.Instance.StageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new Exception();
            }

            SubscribeInventory();

            _isEventDungeonMode = false;
            _worldId = worldId;
            _stageRow = stageRow;
            _apStoneCount.SetValueAndForceNotify(0);
            _ap.SetValueAndForceNotify((int)ReactiveAvatarState.ActionPoint);
            _ticketCount.SetValueAndForceNotify(0);
            _cp.SetValueAndForceNotify(Util.TotalCP(BattleType.Adventure));
            _repeatBattleAction = repeatBattleAction;
            var disableRepeat = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId);
            canvasGroupForRepeat.alpha = disableRepeat ? 0 : 1;
            canvasGroupForRepeat.interactable = !disableRepeat;
            pageToggle.isOn = disableRepeat;
            UpdateByToggle(disableRepeat);
            contentText.text =
                $"({L10nManager.Localize("UI_AP")} / {L10nManager.Localize("UI_AP_POTION")})";

            base.Show(ignoreShowAnimation);
        }

        public void ShowEventDungeon(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            bool ignoreShowAnimation = false)
        {
            if (!TableSheets.Instance.EventDungeonStageSheet.TryGetValue(eventDungeonStageId, out var eventDungeonStageRow))
            {
                throw new Exception($"Event dungeon stage not found: {eventDungeonStageId}");
            }

            SubscribeEventDungeonInventory();

            _isEventDungeonMode = true;
            _eventScheduleId = eventScheduleId;
            _eventDungeonId = eventDungeonId;
            _eventDungeonStageId = eventDungeonStageId;
            _eventDungeonStageRow = eventDungeonStageRow;
            _ticketCount.SetValueAndForceNotify(0);
            _cp.SetValueAndForceNotify(Util.TotalCP(BattleType.Adventure));

            // Clear AP related texts for event dungeon mode
            totalApText.text = string.Empty;
            apStoneText.text = string.Empty;

            // Event dungeon mode - hide repeat battle option
            canvasGroupForRepeat.alpha = 0;
            canvasGroupForRepeat.interactable = false;
            pageToggle.isOn = true; // Always use sweep mode for event dungeon
            UpdateByToggle(true);
            contentText.text = $"{L10nManager.Localize("UI_EVENT_DUNGEON_TICKET")} / {L10nManager.Localize("UI_EVENT_DUNGEON_TICKET")}";

            base.Show(ignoreShowAnimation);
        }

        private void UpdateByToggle(bool useSweep)
        {
            if (_isEventDungeonMode)
            {
                // Event dungeon mode - show event dungeon UI elements
                objectsForSweep.ForEach(obj => obj.SetActive(false));
                objectsForRepeat.ForEach(obj => obj.SetActive(false));
                point.SetActive(false);
                objectsForEventDungeon.ForEach(obj => obj.SetActive(true));
                potion.SetActive(false);
                _useSweep = true; // Always use sweep for event dungeon

                // Disable AP stone slider for event dungeon
                apStoneSlider.Set(0, 0, 0, 0, 1, x => _apStoneCount.Value = x);

                // Enable and setup event dungeon ticket slider
                eventDungeonTicketSlider.gameObject.SetActive(true);
                var ticketProgress = RxProps.EventDungeonTicketProgress.Value;
                var maxTickets = Math.Min(ticketProgress.currentTickets, 100); // Max 100 tickets per sweep
                // Preserve current slider value when initializing
                var currentValue = _ticketCount.Value;
                var clampedValue = Math.Min(currentValue, maxTickets);

                // Set method: (sliderMinValue, sliderMaxValue, sliderCurValue, max, multiplier, callback)
                // For event dungeon tickets: use actual ticket count as max value
                eventDungeonTicketSlider.Set(0, maxTickets, clampedValue, maxTickets, 1,
                    x => _ticketCount.Value = x);
                descriptionText.text = L10nManager.Localize("UI_EVENT_DUNGEON_TICKET_DESCRIPTION");
                haveText.text = L10nManager.Localize("UI_EVENT_DUNGEON_TICKET_HAVE");
            }
            else
            {
                // Normal mode - show regular sweep/repeat UI elements
                objectsForEventDungeon.ForEach(obj => obj.SetActive(false));
                objectsForSweep.ForEach(obj => obj.SetActive(useSweep));
                objectsForRepeat.ForEach(obj => obj.SetActive(!useSweep));
                point.SetActive(true);
                potion.SetActive(true);
                _useSweep = useSweep;

                // Disable event dungeon ticket slider for normal mode
                eventDungeonTicketSlider.gameObject.SetActive(false);

                var materialSheet = TableSheets.Instance.MaterialItemSheet;
                var haveApStoneCount =
                    States.Instance.CurrentAvatarState.inventory.GetUsableItemCount(
                        materialSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone).Id,
                        Game.Game.instance.Agent?.BlockIndex ?? -1);
                apStoneSlider.Set(0,
                    Math.Min(haveApStoneCount, MaxApStoneCount),
                    0,
                    MaxApStoneCount, 1,
                    x => _apStoneCount.Value = x);

                // Setup AP slider for normal mode
                var haveApCount = ReactiveAvatarState.ActionPoint;
                haveApText.text = haveApCount.ToString();
                haveApStoneText.text = haveApStoneCount.ToString();

                _costAp = States.Instance.StakingLevel > 0
                    ? TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                        _stageRow.CostAP, 1, States.Instance.StakingLevel)
                    : _stageRow.CostAP;
                apSlider.Set(0,
                    (int)(haveApCount / _costAp),
                    (int)haveApCount / _costAp,
                    States.Instance.GameConfigState.ActionPointMax,
                    _costAp,
                    x => _ap.Value = x * _costAp);
                descriptionText.text = L10nManager.Localize("UI_BOOSTER_POPUP_DESCRIPTION");
                haveText.text = L10nManager.Localize("UI_AP_YOU_HAVE");
            }

            UpdateView();
        }

        private void SubscribeInventory()
        {
            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                if (inventory is null)
                {
                    return;
                }

                // Only setup AP sliders for normal mode, not event dungeon mode
                if (!_isEventDungeonMode)
                {
                    var materialSheet = TableSheets.Instance.MaterialItemSheet;
                    var haveApStoneCount = inventory.GetUsableItemCount(
                        materialSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone).Id,
                        Game.Game.instance.Agent?.BlockIndex ?? -1);

                    var haveApCount = ReactiveAvatarState.ActionPoint;
                    haveApText.text = haveApCount.ToString();
                    haveApStoneText.text = haveApStoneCount.ToString();

                    _costAp = States.Instance.StakingLevel > 0
                        ? TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                            _stageRow.CostAP, 1, States.Instance.StakingLevel)
                        : _stageRow.CostAP;
                    apSlider.Set(0,
                        (int)(haveApCount / _costAp),
                        (int)haveApCount / _costAp,
                        States.Instance.GameConfigState.ActionPointMax,
                        _costAp,
                        x => _ap.Value = x * _costAp);

                    apStoneSlider.Set(0,
                        Math.Min(haveApStoneCount, MaxApStoneCount),
                        0,
                        MaxApStoneCount, 1,
                        x => _apStoneCount.Value = x);
                }

                _cp.Value = Util.TotalCP(BattleType.Adventure);
            }).AddTo(_disposables);
        }

        private void SubscribeEventDungeonInventory()
        {
            _disposables.DisposeAllAndClear();

            // Subscribe to event dungeon ticket progress
            RxProps.EventDungeonTicketProgress.Subscribe(ticketProgress =>
            {
                haveEventDungeonTicketText.text = ticketProgress.currentTickets.ToString();

                if (_isEventDungeonMode)
                {
                    var maxTickets = Math.Min(ticketProgress.currentTickets, 100);
                    // Preserve current slider value when updating max tickets
                    var currentValue = _ticketCount.Value;
                    // Clamp current value to new max if it exceeds
                    var clampedValue = Math.Min(currentValue, maxTickets);

                    // Set method: (sliderMinValue, sliderMaxValue, sliderCurValue, max, multiplier, callback)
                    // For event dungeon tickets: use actual ticket count as max value
                    eventDungeonTicketSlider.Set(0, maxTickets, clampedValue, maxTickets, 1,
                        x => _ticketCount.Value = x);
                }
            }).AddTo(_disposables);

            // Subscribe to CP changes
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                if (inventory is null) return;
                _cp.Value = Util.TotalCP(BattleType.Adventure);
            }).AddTo(_disposables);

            // Disable AP stone related UI for event dungeon
            haveApStoneText.text = "0";
            apStoneText.text = "";
        }

        private void UpdateCpView()
        {
            if (_stageRow is null)
            {
                return;
            }

            if (!TryGetRequiredCP(_stageRow.Id, out var row))
            {
                return;
            }

            if (_cp.Value < row.RequiredCP)
            {
                enoughCpContainer.SetActive(false);
                insufficientCpContainer.SetActive(true);
                insufficientCpText.text = L10nManager.Localize("UI_SWEEP_CP", row.RequiredCP);
            }
            else
            {
                enoughCpContainer.SetActive(true);
                insufficientCpContainer.SetActive(false);
                enoughCpText.text = L10nManager.Localize("UI_SWEEP_CP", row.RequiredCP);
            }

            UpdateStartButton();
        }


        private void UpdateView()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            if (_isEventDungeonMode)
            {
                UpdateEventDungeonView(avatarState);
                // Event dungeon doesn't need CP check, so hide CP containers
                enoughCpContainer.SetActive(false);
                insufficientCpContainer.SetActive(false);
            }
            else
            {
                var (apPlayCount, apStonePlayCount) =
                    GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value, States.Instance.StakingLevel);
                UpdateRewardView(avatarState, _stageRow, apPlayCount, apStonePlayCount);

                var totalPlayCount = apPlayCount + apStonePlayCount;
                if (_apStoneCount.Value == 0 && _ap.Value == 0)
                {
                    information.SetActive(true);
                    totalApText.text = string.Empty;
                    apStoneText.text = string.Empty;
                }
                else
                {
                    information.SetActive(false);
                    totalApText.text = totalPlayCount.ToString();
                    apStoneText.text = apStonePlayCount > 0
                        ? $"(+{apStonePlayCount})"
                        : string.Empty;
                }

                // Update CP view for normal mode
                UpdateCpView();
            }

            UpdateStartButton();
        }

        private void UpdateEventDungeonView(AvatarState avatarState)
        {
            var playCount = _ticketCount.Value;
            UpdateEventDungeonRewardView(avatarState, _eventDungeonStageRow, playCount);

            // Clear AP related texts for event dungeon mode
            totalApText.text = string.Empty;
            apStoneText.text = string.Empty;

            if (playCount == 0)
            {
                information.SetActive(true);
                eventDungeonTicketText.text = string.Empty;
            }
            else
            {
                information.SetActive(false);
                eventDungeonTicketText.text = playCount.ToString();
            }
        }

        private void UpdateRewardView(
            AvatarState avatarState,
            StageSheet.Row row,
            int apPlayCount,
            int apStonePlayCount)
        {
            var earnedExp = GetEarnedExp(avatarState,
                row,
                apPlayCount,
                apStonePlayCount);
            var maxStar = Math.Min((apPlayCount + apStonePlayCount) * 2,
                TableSheets.Instance.CrystalStageBuffGachaSheet[row.Id].MaxStar);
            expText.text = $"+{earnedExp}";
            starText.text = $"+{maxStar}";
            expGlow.SetActive(earnedExp > 0);
        }

        private void UpdateEventDungeonRewardView(
            AvatarState avatarState,
            EventDungeonStageSheet.Row row,
            int playCount)
        {
            var earnedExp = GetEventDungeonEarnedExp(avatarState, row, playCount);
            var maxStar = Math.Min(playCount * 2, 100); // Event dungeon stars calculation
            expText.text = $"+{earnedExp}";
            starText.text = $"+{maxStar}";
            expGlow.SetActive(earnedExp > 0);
        }

        private static bool TryGetRequiredCP(int stageId, out SweepRequiredCPSheet.Row row)
        {
            return TableSheets.Instance.SweepRequiredCPSheet.TryGetValue(stageId, out row);
        }

        private static (int, int) GetPlayCount(
            StageSheet.Row row,
            int apStoneCount,
            int ap,
            int stakingLevel)
        {
            if (row is null)
            {
                return (0, 0);
            }

            var actionMaxPoint = States.Instance.GameConfigState.ActionPointMax;
            var costAp = row.CostAP;
            if (stakingLevel > 0)
            {
                costAp = TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                    costAp, 1, stakingLevel);
            }

            var apStonePlayCount = actionMaxPoint / costAp * apStoneCount;
            var apPlayCount = ap / costAp;
            return (apPlayCount, apStonePlayCount);
        }

        private long GetEarnedExp(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var levelSheet = TableSheets.Instance.CharacterLevelSheet;
            var (_, exp) = avatarState.GetLevelAndExp(levelSheet, row.Id,
                apPlayCount + apStonePlayCount);
            var earnedExp = exp - avatarState.exp;
            return Math.Max(earnedExp, 0);
        }

        private long GetEventDungeonEarnedExp(AvatarState avatarState, EventDungeonStageSheet.Row row, int playCount)
        {
            // Event dungeon experience calculation
            // Use the event schedule to get proper experience calculation
            var scheduleRow = RxProps.EventScheduleRowForDungeon.Value;
            if (scheduleRow != null)
            {
                var eventExp = scheduleRow.GetStageExp(
                    _eventDungeonStageId.ToEventDungeonStageNumber(),
                    playCount);
                return Math.Max(eventExp, 0);
            }

            // Fallback to regular calculation if schedule is not available
            var levelSheet = TableSheets.Instance.CharacterLevelSheet;
            var (_, totalExp) = avatarState.GetLevelAndExp(levelSheet, row.Id, playCount);
            var earnedExp = totalExp - avatarState.exp;
            return Math.Max(earnedExp, 0);
        }

        private void UpdateStartButton()
        {
            if (_isEventDungeonMode)
            {
                if (_ticketCount.Value == 0)
                {
                    startButton.Interactable = false;
                    return;
                }

                // Event dungeon doesn't have CP requirements like regular stages
                startButton.Interactable = true;
            }
            else
            {
                if (_apStoneCount.Value == 0 && _ap.Value == 0)
                {
                    startButton.Interactable = false;
                    return;
                }

                if (_useSweep && TryGetRequiredCP(_stageRow.Id, out var row))
                {
                    if (_cp.Value < row.RequiredCP)
                    {
                        startButton.Interactable = false;
                        return;
                    }
                }

                startButton.Interactable = true;
            }
        }

        private void Sweep(int apStoneCount, int ap, int worldId, StageSheet.Row stageRow)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var (apPlayCount, apStonePlayCount)
                = GetPlayCount(stageRow, apStoneCount, ap, States.Instance.StakingLevel);
            var totalPlayCount = apPlayCount + apStonePlayCount;
            var actionPoint = apPlayCount * _costAp;
            if (totalPlayCount <= 0)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_SWEEP_PLAY_COUNT_ZERO"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            if (!TryGetRequiredCP(stageRow.Id, out var row))
            {
                NcDebug.LogError($"Not found required CP for stageId: {stageRow.Id}");
                return;
            }

            if (_cp.Value < row.RequiredCP)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_SWEEP_REQUIRED_CP"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            var costumes = States.Instance.CurrentItemSlotStates[BattleType.Adventure].Costumes;
            var equipments = States.Instance.CurrentItemSlotStates[BattleType.Adventure].Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();
            Game.Game.instance.ActionManager.HackAndSlashSweep(
                costumes,
                equipments,
                runeInfos,
                apStoneCount,
                actionPoint,
                worldId,
                stageRow.Id,
                totalPlayCount).Subscribe();

            Close();

            var earnedExp = GetEarnedExp(avatarState, stageRow, apPlayCount, apStonePlayCount);

            Find<SweepResultPopup>().Show(stageRow, worldId, apPlayCount, apStonePlayCount, earnedExp);
        }

        private void EventDungeonSweep(int ticketCount)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var playCount = ticketCount;

            if (playCount <= 0)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_SWEEP_PLAY_COUNT_ZERO"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            if (playCount > 100)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_EVENT_DUNGEON_MAX_PLAY_COUNT"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            var costumes = States.Instance.CurrentItemSlotStates[BattleType.Adventure].Costumes;
            var equipments = States.Instance.CurrentItemSlotStates[BattleType.Adventure].Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();

            // Event dungeon sweep uses only tickets, no AP stones
            Game.Game.instance.ActionManager.EventDungeonBattleSweep(
                _eventScheduleId,
                _eventDungeonId,
                _eventDungeonStageId,
                equipments,
                costumes,
                new List<Guid>(), // Foods - empty for sweep
                runeInfos,
                playCount).Subscribe();

            Close();

            var earnedExp = GetEventDungeonEarnedExp(avatarState, _eventDungeonStageRow, playCount);

            Find<SweepResultPopup>().ShowEventDungeon((Nekoyume.TableData.Event.EventDungeonStageSheet.Row)_eventDungeonStageRow, _eventDungeonId, playCount, earnedExp);
        }
    }
}
