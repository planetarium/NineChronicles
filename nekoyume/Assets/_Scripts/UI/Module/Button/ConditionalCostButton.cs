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
    }
}
