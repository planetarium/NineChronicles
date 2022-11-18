using System;
using System.Collections.Generic;
using mixpanel;
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

        private readonly ReactiveProperty<int> _apStoneCount = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _ap = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _cp = new ReactiveProperty<int>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private StageSheet.Row _stageRow;
        private int _worldId;
        private int _costAp;
        private bool _useSweep = true;
        private Action<StageType, int, bool> _repeatBattleAction;

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
                        _repeatBattleAction(
                            StageType.HackAndSlash,
                            _ap.Value / _costAp,
                            false);
                        Close();
                    }
                })
                .AddTo(gameObject);

            cancelButton.onClick.AddListener(() => Close());

            CloseWidget = () => { Close(); };

            base.Awake();
        }

        public void Show(
            int worldId,
            int stageId,
            Action<StageType, int, bool> repeatBattleAction,
            bool ignoreShowAnimation = false)
        {
            if (!Game.Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new Exception();
            }

            SubscribeInventory();

            _worldId = worldId;
            _stageRow = stageRow;
            _apStoneCount.SetValueAndForceNotify(0);
            _ap.SetValueAndForceNotify(States.Instance.CurrentAvatarState.actionPoint);
            _cp.SetValueAndForceNotify(Util.TotalCP(BattleType.Adventure));
            _repeatBattleAction = repeatBattleAction;
            var disableRepeat = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId);
            canvasGroupForRepeat.alpha = disableRepeat ? 0 : 1;
            canvasGroupForRepeat.interactable = !disableRepeat;
            pageToggle.isOn = disableRepeat;
            contentText.text =
                $"({L10nManager.Localize("UI_AP")} / {L10nManager.Localize("UI_AP_POTION")})";

            base.Show(ignoreShowAnimation);
        }

        private void UpdateByToggle(bool useSweep)
        {
            objectsForSweep.ForEach(obj => obj.SetActive(useSweep));
            objectsForRepeat.ForEach(obj => obj.SetActive(!useSweep));
            _useSweep = useSweep;
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

                var haveApStoneCount = 0;

                foreach (var item in inventory.Items)
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    switch (item.item.ItemType)
                    {
                        case ItemType.Material:
                            if (item.item.ItemSubType != ItemSubType.ApStone)
                            {
                                continue;
                            }

                            if (item.item is ITradableItem tradableItem)
                            {
                                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                                if (tradableItem.RequiredBlockIndex > blockIndex)
                                {
                                    continue;
                                }

                                haveApStoneCount += item.count;
                            }
                            else
                            {
                                haveApStoneCount += item.count;
                            }

                            break;
                    }
                }

                var haveApCount = States.Instance.CurrentAvatarState.actionPoint;

                haveApText.text = haveApCount.ToString();
                haveApStoneText.text = haveApStoneCount.ToString();

                _costAp = States.Instance.StakingLevel > 0
                    ? TableSheets.Instance.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                        _stageRow.CostAP, 1, States.Instance.StakingLevel)
                    : _stageRow.CostAP;
                apSlider.Set(0,
                    haveApCount / _costAp,
                    haveApCount / _costAp,
                    States.Instance.GameConfigState.ActionPointMax,
                    _costAp,
                    x => _ap.Value = x * _costAp);

                apStoneSlider.Set(0,
                    Math.Min(haveApStoneCount, HackAndSlashSweep.UsableApStoneCount),
                    0,
                    HackAndSlashSweep.UsableApStoneCount, 1,
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
                totalApText.text = (_useSweep ? totalPlayCount : apPlayCount).ToString();
                apStoneText.text = apStonePlayCount > 0 && _useSweep
                    ? $"(+{apStonePlayCount})"
                    : string.Empty;
            }

            UpdateStartButton();
        }

        private void UpdateRewardView(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var earnedExp = GetEarnedExp(avatarState,
                row,
                apPlayCount,
                _useSweep ? apStonePlayCount : 0);
            expText.text = $"+{earnedExp}";
            starText.text = $"+{apPlayCount * 2}";
            expGlow.SetActive(earnedExp > 0);
        }

        private static bool TryGetRequiredCP(int stageId, out SweepRequiredCPSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.SweepRequiredCPSheet;
            return sheet.TryGetValue(stageId, out row);
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
            var levelSheet = Game.Game.instance.TableSheets.CharacterLevelSheet;
            var (_, exp) = avatarState.GetLevelAndExp(levelSheet, row.Id,
                apPlayCount + apStonePlayCount);
            var earnedExp = exp - avatarState.exp;
            return earnedExp;
        }

        private void UpdateStartButton()
        {
            if (!_useSweep)
            {
                startButton.Interactable = _ap.Value > 0;
                return;
            }

            if (_apStoneCount.Value == 0 && _ap.Value == 0)
            {
                startButton.Interactable = false;
                return;
            }

            if (TryGetRequiredCP(_stageRow.Id, out var row))
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
                return;
            }

            if (_cp.Value < row.RequiredCP)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_SWEEP_REQUIRED_CP"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            var costumes = States.Instance.ItemSlotStates[BattleType.Adventure].Costumes;
            var equipments = States.Instance.ItemSlotStates[BattleType.Adventure].Equipments;
            var runeInfos = States.Instance.RuneSlotStates[BattleType.Adventure]
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

            Find<SweepResultPopup>()
                .Show(stageRow, worldId, apPlayCount, apStonePlayCount, earnedExp);
        }
    }
}
