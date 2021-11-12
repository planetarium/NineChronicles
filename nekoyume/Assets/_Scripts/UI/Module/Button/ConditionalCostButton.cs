using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
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
            NCG,
            ActionPoint
        }

        [SerializeField]
        private List<TextMeshProUGUI> costTexts = null;

        [SerializeField]
        private CostType costType = CostType.NCG;

        private int _cost;

        public void SetCost(int value)
        {
            _cost = value;
            foreach (var text in costTexts)
            {
                text.text = value.ToString();
            }
        }

        protected override bool CheckCondition()
        {
            switch (costType)
            {
                case CostType.NCG:
                    return States.Instance.GoldBalanceState.Gold.MajorUnit >= _cost
                        && base.CheckCondition();
                case CostType.ActionPoint:
                    return States.Instance.CurrentAvatarState.actionPoint >= _cost
                        && base.CheckCondition();
                default:
                    return base.CheckCondition();
            }
        }

        protected override void OnClickButton()
        {
            switch (costType)
            {
                case CostType.NCG:
                    if (States.Instance.GoldBalanceState.Gold.MajorUnit < _cost)
                    {
                        OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_NOT_ENOUGH_NCG"));
                        return;
                    }
                    break;
                case CostType.ActionPoint:
                    if (States.Instance.CurrentAvatarState.actionPoint < _cost)
                    {
                        OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_NOT_ENOUGH_AP"));
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
