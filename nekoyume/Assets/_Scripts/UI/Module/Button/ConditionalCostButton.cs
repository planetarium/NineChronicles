using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
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
            Hourglass,
            Crystal
        }

        [SerializeField]
        private List<TextMeshProUGUI> costTexts = null;

        private CostType _costType = CostType.NCG;

        public int Cost { get; private set; } = int.MaxValue;

        public Color CostTextColor
        {
            get
            {
                var text = costTexts.FirstOrDefault();
                return text is null ? Color.white : text.color;
            }
            set
            {
                foreach (var text in costTexts)
                {
                    text.color = value;
                }
            }
        }

        public void SetCost(CostType costType, int value)
        {
            Cost = value;
            _costType = costType;
            foreach (var text in costTexts)
            {
                text.text = value.ToString();
            }
            UpdateObjects();
        }

        public override void UpdateObjects()
        {
            base.UpdateObjects();
            CostTextColor = CheckCost() ? Palette.GetColor(ColorType.ButtonEnabled) :
                Palette.GetColor(ColorType.ButtonDisabled);
        }

        protected bool CheckCost()
        {
            switch (_costType)
            {
                case CostType.None:
                    Debug.LogError("Cost not set!");
                    return false;
                case CostType.NCG:
                    return States.Instance.GoldBalanceState.Gold.MajorUnit >= Cost;
                case CostType.ActionPoint:
                    return States.Instance.CurrentAvatarState.actionPoint >= Cost;
                case CostType.Hourglass:
                    var inventory = States.Instance.CurrentAvatarState.inventory;
                    var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
                    return count >= Cost;
                default:
                    return true;
            }
        }

        protected override bool CheckCondition()
        {
            return CheckCost() && base.CheckCondition();
        }

        protected override void OnClickButton()
        {
            switch (_costType)
            {
                case CostType.NCG:
                    if (States.Instance.GoldBalanceState.Gold.MajorUnit < Cost)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                    break;
                case CostType.ActionPoint:
                    if (States.Instance.CurrentAvatarState.actionPoint < Cost)
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
                    if (count < Cost)
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
