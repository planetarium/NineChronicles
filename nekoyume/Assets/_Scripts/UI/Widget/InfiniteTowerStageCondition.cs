using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class InfiniteTowerStageCondition : Widget
    {
        [SerializeField] private InfiniteTowerConditionItem[] conditionItems;  // 개별 조건 아이템들

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
        }

        public void SetConditions(List<InfiniteTowerCondition> conditions)
        {
            foreach (var conditionItem in conditionItems)
            {
                conditionItem.gameObject.SetActive(false);
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                var conditionItem = conditionItems[i];
                conditionItem.gameObject.SetActive(true);
                conditionItem.SetSelectedCondition(conditions[i]);
            }
        }
    }
}
