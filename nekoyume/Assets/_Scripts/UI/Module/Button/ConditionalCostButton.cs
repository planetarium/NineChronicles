using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ConditionalCostButton : ConditionalButton
    {
        public enum CostType
        {
            None,
            NCG,
            ActionPoint,
            Hourglass
        }

        [SerializeField]
        private List<TextMeshProUGUI> costTexts = null;

        private CostType _costType = CostType.NCG;

        private int _cost = int.MaxValue;

        public void SetCost(CostType costType, int value)
        {
            _cost = value;
            _costType = costType;
            foreach (var text in costTexts)
            {
                text.text = value.ToString();
            }
            UpdateObjects();
        }

        protected override bool CheckCondition()
        {
            switch (_costType)
            {
                case CostType.None:
                    Debug.LogError("Cost not set!");
                    return false;
                case CostType.NCG:
                    return States.Instance.GoldBalanceState.Gold.MajorUnit >= _cost
                        && base.CheckCondition();
                case CostType.ActionPoint:
                    return States.Instance.CurrentAvatarState.actionPoint >= _cost
                        && base.CheckCondition();
                case CostType.Hourglass:
                    var inventory = States.Instance.CurrentAvatarState.inventory;
                    var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
                    return count >= _cost && base.CheckCondition();
                default:
                    return base.CheckCondition();
            }
        }

        protected override void OnClickButton()
        {
            switch (_costType)
            {
                case CostType.NCG:
                    if (States.Instance.GoldBalanceState.Gold.MajorUnit < _cost)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                    break;
                case CostType.ActionPoint:
                    if (States.Instance.CurrentAvatarState.actionPoint < _cost)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_NOT_ENOUGH_AP"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                    break;
                case CostType.Hourglass:
                    var inventory = States.Instance.CurrentAvatarState.inventory;
                    var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
                    if (count < _cost)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_NOT_ENOUGH_HOURGLASS"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                    break;
                default:
                    break;
            }

            base.OnClickButton();
        }
    }
}
