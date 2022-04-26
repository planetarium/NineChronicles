using System;
using System.Collections.Generic;
using System.Linq;
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
        private ConditionalButton startButton;

        [SerializeField]
        private ConditionalButton cancelButton;

        [SerializeField]
        private List<Button> addCountButtons;

        [SerializeField]
        private TextMeshProUGUI playCountText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI apText;

        [SerializeField]
        private TextMeshProUGUI inventoryApStoneText;

        [SerializeField]
        private TextMeshProUGUI useApStoneText;

        private readonly ReactiveProperty<int> _apStoneCount = new ReactiveProperty<int>();
        private StageSheet.Row _stageRow;
        private long _gainedExp;
        private int _worldId;
        private int _inventoryApStoneCount;

        protected override void Awake()
        {
            _apStoneCount.Subscribe(v => UpdateView()).AddTo(gameObject);

            startButton.Text = L10nManager.Localize("UI_START");
            startButton.OnSubmitSubject
                .Subscribe(_ => Sweep(_apStoneCount.Value, _worldId, _stageRow)).AddTo(gameObject);

            cancelButton.Text = L10nManager.Localize("UI_CANCEL");
            cancelButton.OnSubmitSubject.Subscribe(_ => Close()).AddTo(gameObject);

            var counts = new[]
                { -HackAndSlashSweep.UsableApStoneCount, 1, HackAndSlashSweep.UsableApStoneCount };
            for (var i = 0; i < addCountButtons.Count; i++)
            {
                var value = counts[i];
                addCountButtons[i].onClick.AddListener(() => { UpdateApStoneCount(value); });
            }

            base.Awake();
        }

        public void Show(int worldId, int stageId, bool ignoreShowAnimation = false)
        {
            if (!Game.Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new Exception();
            }

            _worldId = worldId;
            _stageRow = stageRow;

            var avatarState = States.Instance.CurrentAvatarState;
            var apStoneRow = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            if (avatarState.inventory.TryGetFungibleItems(apStoneRow.ItemId, out var items))
            {
                _inventoryApStoneCount = items.Sum(x => x.count);
                inventoryApStoneText.text = _inventoryApStoneCount.ToString();
            }

            apText.text = avatarState.actionPoint.ToString();
            _apStoneCount.SetValueAndForceNotify(0);
            base.Show(ignoreShowAnimation);
        }

        private void UpdateApStoneCount(int value)
        {
            var current = Mathf.Min(_apStoneCount.Value + value, _inventoryApStoneCount);
            current = Mathf.Clamp(current, 0, HackAndSlashSweep.UsableApStoneCount);
            if (current != 0 && current == _apStoneCount.Value)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("NO_LONGER_AVAILABLE"),
                    NotificationCell.NotificationType.Notification);
            }

            _apStoneCount.Value = current;
        }

        private void UpdateView()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            var (apPlayCount, apStonePlayCount) = GetPlayCount(avatarState, _stageRow);
            playCountText.text = (apPlayCount + apStonePlayCount).ToString();

            var levelSheet = Game.Game.instance.TableSheets.CharacterLevelSheet;
            var (_, exp) = avatarState.GetLevelAndExp(levelSheet, _stageRow.Id,
                apPlayCount + apStonePlayCount);

            _gainedExp = exp - avatarState.exp;

            var actionMaxPoint = States.Instance.GameConfigState.ActionPointMax;
            apText.text = apStonePlayCount > 0
                ? $"{avatarState.actionPoint}<color=#39FD39>(+{_apStoneCount.Value * actionMaxPoint})</color>"
                : $"{avatarState.actionPoint}";
            expText.text = _gainedExp.ToString();

            useApStoneText.text = $"{_apStoneCount.Value}/{HackAndSlashSweep.UsableApStoneCount}";
        }

        private (int, int) GetPlayCount(AvatarState avatarState, StageSheet.Row row)
        {
            if (row is null)
            {
                return (0, 0);
            }

            var actionMaxPoint = States.Instance.GameConfigState.ActionPointMax;
            var apStonePlayCount = actionMaxPoint / row.CostAP * _apStoneCount.Value;
            var apPlayCount = avatarState.actionPoint / row.CostAP;
            return (apPlayCount, apStonePlayCount);
        }

        private void Sweep(int apStoneCount, int worldId, StageSheet.Row stageRow)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var (apPlayCount, apStonePlayCount) = GetPlayCount(avatarState, stageRow);
            if (apPlayCount + apStonePlayCount <= 0)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_SWEEP_PLAY_COUNT_ZERO"),
                    NotificationCell.NotificationType.Notification);
                return;
            }

            var apStoneRow = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            LocalLayerModifier.RemoveItem(avatarState.address, apStoneRow.ItemId, apStoneCount);
            Game.Game.instance.ActionManager.HackAndSlashSweep(apStoneCount, worldId, stageRow.Id);
            Close();
            Find<SweepResultPopup>().Show(stageRow, worldId, apPlayCount + apStonePlayCount, _gainedExp);
        }
    }
}
