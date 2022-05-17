using System;
using System.Collections.Generic;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
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
        private TextMeshProUGUI playCountText;

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
        private GameObject playCountGlow;

        private readonly ReactiveProperty<int> _apStoneCount = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _ap = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _cp = new ReactiveProperty<int>();
        private readonly List<Guid> equipments = new List<Guid>();
        private readonly List<Guid> costumes = new List<Guid>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private StageSheet.Row _stageRow;
        private int _worldId;

        protected override void Awake()
        {
            _apStoneCount.Subscribe(v => UpdateView()).AddTo(gameObject);
            _ap.Subscribe(v => UpdateView()).AddTo(gameObject);
            _cp.Subscribe(v => UpdateCpView()).AddTo(gameObject);

            startButton.OnSubmitSubject
                .Subscribe(_ => Sweep(_apStoneCount.Value, _ap.Value, _worldId, _stageRow))
                .AddTo(gameObject);

            cancelButton.onClick.AddListener(() => Close());

            CloseWidget = () => { Close(); };

            base.Awake();
        }

        public void Show(int worldId, int stageId, bool ignoreShowAnimation = false)
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
            _cp.SetValueAndForceNotify(States.Instance.CurrentAvatarState.GetCP());

            contentText.text =
                $"({L10nManager.Localize("UI_AP")} / {L10nManager.Localize("UI_AP_POTION")})";

            base.Show(ignoreShowAnimation);
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
                costumes.Clear();
                equipments.Clear();

                foreach (var item in inventory.Items)
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    switch (item.item.ItemType)
                    {
                        case ItemType.Costume:
                            var costume = (Costume)item.item;
                            if (costume.equipped)
                            {
                                costumes.Add(costume.ItemId);
                            }

                            break;

                        case ItemType.Equipment:
                            var equipment = (Equipment)item.item;
                            if (equipment.equipped)
                            {
                                equipments.Add(equipment.ItemId);
                            }

                            break;

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

                apSlider.Set(haveApCount / _stageRow.CostAP, haveApCount / _stageRow.CostAP,
                    States.Instance.GameConfigState.ActionPointMax, _stageRow.CostAP,
                    x => _ap.Value = x * _stageRow.CostAP);

                apStoneSlider.Set(Math.Min(haveApStoneCount, HackAndSlashSweep.UsableApStoneCount),
                    0,
                    HackAndSlashSweep.UsableApStoneCount, 1,
                    x => _apStoneCount.Value = x);

                _cp.Value = States.Instance.CurrentAvatarState.GetCP();
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
                GetPlayCount(_stageRow, _apStoneCount.Value, _ap.Value);
            var totalPlayCount = apPlayCount + apStonePlayCount;

            playCountText.text = totalPlayCount.ToString();
            playCountGlow.SetActive(totalPlayCount > 0);

            UpdateExpView(avatarState, _stageRow, apPlayCount, apStonePlayCount);

            if (_apStoneCount.Value == 0 && _ap.Value == 0)
            {
                information.SetActive(true);
                totalApText.text = string.Empty;
                apStoneText.text = string.Empty;
            }
            else
            {
                information.SetActive(false);
                totalApText.text = $"{totalPlayCount * _stageRow.CostAP}";
                apStoneText.text = apStonePlayCount > 0
                    ? $"(+{apStonePlayCount * _stageRow.CostAP})"
                    : string.Empty;
            }

            UpdateStartButton();
        }

        private void UpdateExpView(AvatarState avatarState, StageSheet.Row row, int apPlayCount,
            int apStonePlayCount)
        {
            var earnedExp = GetEarnedExp(avatarState, row, apPlayCount, apStonePlayCount);
            expText.text = $"+{earnedExp}";
            expGlow.SetActive(earnedExp > 0);
        }

        private static bool TryGetRequiredCP(int stageId, out SweepRequiredCPSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.SweepRequiredCPSheet;
            return sheet.TryGetValue(stageId, out row);
        }

        private static (int, int) GetPlayCount(StageSheet.Row row, int apStoneCount, int ap)
        {
            if (row is null)
            {
                return (0, 0);
            }

            var actionMaxPoint = States.Instance.GameConfigState.ActionPointMax;
            var apStonePlayCount = actionMaxPoint / row.CostAP * apStoneCount;
            var apPlayCount = ap / row.CostAP;
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
            var (apPlayCount, apStonePlayCount) = GetPlayCount(stageRow, apStoneCount, ap);
            var totalPlayCount = apPlayCount + apStonePlayCount;
            var actionPoint = apPlayCount * stageRow.CostAP;
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

            Game.Game.instance.ActionManager.HackAndSlashSweep(
                costumes,
                equipments,
                apStoneCount,
                actionPoint,
                worldId,
                stageRow.Id);

            Analyzer.Instance.Track("Unity/HackAndSlashSweep", new Value
            {
                ["stageId"] = stageRow.Id,
                ["apStoneCount"] = apStoneCount,
                ["playCount"] = totalPlayCount,
            });

            Close();

            var earnedExp = GetEarnedExp(avatarState, stageRow, apPlayCount, apStonePlayCount);

            Find<SweepResultPopup>()
                .Show(stageRow, worldId, apPlayCount, apStonePlayCount, earnedExp);
        }
    }
}
