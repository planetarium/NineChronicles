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
        private TextMeshProUGUI haveApText;

        [SerializeField]
        private TextMeshProUGUI haveApStoneText;

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
        private GameObject apPotionContainer;

        [SerializeField]
        private Image apImage;

        [SerializeField]
        private Image ticketImage;

        private readonly ReactiveProperty<int> _apStoneCount = new();
        private readonly ReactiveProperty<int> _ap = new();
        private readonly ReactiveProperty<long> _cp = new();
        private readonly List<IDisposable> _disposables = new();

        private StageSheet.Row _stageRow;
        private int _worldId;
        private int _costAp;
        private bool _useSweep = true;
        private Action<StageType, int, int, bool> _repeatBattleAction;
        private StageType _stageType;
        private int _stageId;

        private const int UsableApStoneCountWithRepeat = 1;

        private int MaxApStoneCount =>
            _useSweep
                ? HackAndSlashSweep.UsableApStoneCount
                : UsableApStoneCountWithRepeat;

        protected override void Awake()
        {
            _apStoneCount.Subscribe(v => UpdateView()).AddTo(gameObject);
            _ap.Subscribe(v => UpdateView()).AddTo(gameObject);
            _cp.Subscribe(v => UpdateCpView()).AddTo(gameObject);
            pageToggle.onValueChanged.AddListener(UpdateByToggle);

            startButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    if (_useSweep)
                    {
                        Sweep(_apStoneCount.Value, _ap.Value, _worldId, _stageRow);
                    }
                    else
                    {
                        var (count1, count2) = GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value,
                            States.Instance.StakingLevel, _stageType);
                        _repeatBattleAction(
                            _stageType,
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
            Debug.Log($"SweepPopup.Show called - WorldId: {worldId}, StageId: {stageId}");

            // 스테이지 타입 확인
            _stageType = DetermineStageType(stageId);
            _stageId = stageId;
            Debug.Log($"Determined StageType: {_stageType}");

            if (_stageType == StageType.EventDungeon)
            {
                if (!TableSheets.Instance.EventDungeonStageSheet.TryGetValue(stageId, out var stageRow))
                {
                    Debug.LogError($"Stage not found in StageSheet: {stageId}");
                    throw new Exception();
                }

                var haveApCount = RxProps.EventDungeonTicketProgress.Value.currentTickets;
                haveApText.text = haveApCount.ToString();
                // ticket count
                _costAp = 1;
                apSlider.Set(0,
                    haveApCount / _costAp,
                    haveApCount / _costAp,
                    RxProps.EventDungeonTicketProgress.Value.maxTickets,
                    _costAp,
                    x => _ap.Value = x * _costAp);

                apStoneSlider.Set(0,
                    0,
                    0,
                    MaxApStoneCount, 1,
                    x => _apStoneCount.Value = x);

                _cp.Value = Util.TotalCP(BattleType.Adventure);
                _useSweep = false;
                _ap.SetValueAndForceNotify(haveApCount);
            }
            else
            {
                if (!TableSheets.Instance.StageSheet.TryGetValue(stageId, out var stageRow))
                {
                    Debug.LogError($"Stage not found in StageSheet: {stageId}");
                    throw new Exception();
                }

                SubscribeInventory();

                _worldId = worldId;
                _stageRow = stageRow;
                _ap.SetValueAndForceNotify((int)ReactiveAvatarState.ActionPoint);
            }
            _apStoneCount.SetValueAndForceNotify(0);
            _cp.SetValueAndForceNotify(Util.TotalCP(BattleType.Adventure));
            _repeatBattleAction = repeatBattleAction;
            var disableRepeat = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId);
            canvasGroupForRepeat.alpha = disableRepeat ? 0 : 1;
            canvasGroupForRepeat.interactable = !disableRepeat;
            pageToggle.isOn = disableRepeat;
            UpdateByToggle(disableRepeat);
            UpdateUIForStageType();
            UpdateView();

            Debug.Log($"SweepPopup.Show about to call base.Show");
            base.Show(ignoreShowAnimation);
        }

        private void UpdateByToggle(bool useSweep)
        {
            objectsForSweep.ForEach(obj => obj.SetActive(useSweep));
            objectsForRepeat.ForEach(obj => obj.SetActive(!useSweep));
            _useSweep = useSweep;
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

                _cp.Value = Util.TotalCP(BattleType.Adventure);
            }).AddTo(_disposables);
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

            var (apPlayCount, apStonePlayCount) =
                GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value, States.Instance.StakingLevel, _stageType);

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

            UpdateStartButton();
        }

        private void UpdateRewardView(
            AvatarState avatarState,
            StageSheet.Row row,
            int apPlayCount,
            int apStonePlayCount)
        {
            int maxStar = 0;
            long earnedExp = 0L;
            if (_stageType == StageType.EventDungeon)
            {
                // 이벤트 던전에서도 apStonePlayCount를 고려하여 exp 계산
                var totalPlayCount = apPlayCount + apStonePlayCount;
                earnedExp = RxProps.EventScheduleRowForDungeon.Value.GetStageExp(
                    _stageId.ToEventDungeonStageNumber(),
                    totalPlayCount);
            }
            else
            {
                earnedExp = GetStageEarnedExp(avatarState,
                    row,
                    apPlayCount,
                    apStonePlayCount);
                maxStar = Math.Min((apPlayCount + apStonePlayCount) * 2,
                    TableSheets.Instance.CrystalStageBuffGachaSheet[row.Id].MaxStar);
            }
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
            int stakingLevel,
            StageType stageType)
        {
            // 이벤트 던전의 경우 직접 계산
            if (stageType == StageType.EventDungeon)
            {
                return (ap, 0);
            }

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

        private long GetStageEarnedExp(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var levelSheet = TableSheets.Instance.CharacterLevelSheet;
            var (_, exp) = avatarState.GetLevelAndExp(levelSheet, row.Id,
                apPlayCount + apStonePlayCount);
            var earnedExp = exp - avatarState.exp;
            return Math.Max(earnedExp, 0);
        }

        private void UpdateStartButton()
        {
            if (_apStoneCount.Value == 0 && _ap.Value == 0)
            {
                startButton.Interactable = false;
                return;
            }

            if (_useSweep && _stageType != StageType.EventDungeon && TryGetRequiredCP(_stageRow.Id, out var row))
            {
                if (_cp.Value < row.RequiredCP)
                {
                    startButton.Interactable = false;
                    return;
                }
            }

            startButton.Interactable = true;
        }

        private void Sweep(int apStoneCount, int ap, int worldId, StageSheet.Row stageRow)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var (apPlayCount, apStonePlayCount)
                = GetPlayCount(stageRow, apStoneCount, ap, States.Instance.StakingLevel,_stageType);
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

            var earnedExp = GetStageEarnedExp(avatarState, stageRow, apPlayCount, apStonePlayCount);

            Find<SweepResultPopup>().Show(stageRow, worldId, apPlayCount, apStonePlayCount, earnedExp);
        }

        private StageType DetermineStageType(int stageId)
        {
            // 이벤트 던전 스테이지인지 확인
            if (TableSheets.Instance.EventDungeonStageSheet.TryGetValue(stageId, out _))
            {
                return StageType.EventDungeon;
            }

            // 일반 스테이지로 처리
            return StageType.HackAndSlash;
        }

        private void UpdateUIForStageType()
        {
            if (_stageType == StageType.EventDungeon)
            {
                // 이벤트 던전 UI 표시
                contentText.text = $"({L10nManager.Localize("UI_EVENT_DUNGEON_TICKET")})";
                apPotionContainer.SetActive(false);
                apImage.gameObject.SetActive(false);
                ticketImage.gameObject.SetActive(true);

                // 이벤트 던전 관련 UI 요소들 활성화 (필요시 추가)
                // 예: eventDungeonContainer?.SetActive(true);
                //     normalStageContainer?.SetActive(false);
            }
            else
            {
                // 일반 스테이지 UI 표시
                contentText.text = $"({L10nManager.Localize("UI_AP")} / {L10nManager.Localize("UI_AP_POTION")})";
                apPotionContainer.SetActive(true);
                apImage.gameObject.SetActive(true);
                ticketImage.gameObject.SetActive(false);

                // 일반 스테이지 관련 UI 요소들 활성화 (필요시 추가)
                // 예: eventDungeonContainer?.SetActive(false);
                //     normalStageContainer?.SetActive(true);
            }
        }
    }
}
