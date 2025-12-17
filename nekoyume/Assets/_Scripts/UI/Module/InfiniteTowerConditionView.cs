using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class InfiniteTowerConditionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;  // "BATTLE CONDITION" / "BUFF CONDITION"
        [SerializeField] private InfiniteTowerConditionItem[] conditionItems;  // 개별 조건 아이템들
        [SerializeField] private GameObject noConditionsText;  // 조건이 없을 때 표시할 텍스트

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void SetConditions(List<InfiniteTowerBattleCondition> battleConditions,
                                 List<InfiniteTowerCondition> buffConditions,
                                 InfiniteTowerFloorSheet.Row floorData)
        {
            if (conditionItems == null) return;

            // 모든 조건 아이템 비활성화
            foreach (var item in conditionItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                }
            }

            if (noConditionsText != null)
            {
                noConditionsText.SetActive(false);
            }

            var allConditions = new List<object>();
            if (battleConditions != null) allConditions.AddRange(battleConditions);
            if (buffConditions != null) allConditions.AddRange(buffConditions);

            if (allConditions.Count == 0)
            {
                if (noConditionsText != null)
                {
                    noConditionsText.SetActive(true);
                }
                return;
            }

            // Weight 정보 추출 및 비율 계산
            var weightMap = new Dictionary<int, int>();
            var totalWeight = 0;
            if (floorData != null)
            {
                var weightedConditions = floorData.GetRandomConditionsWithWeights();
                foreach (var (conditionId, weight) in weightedConditions)
                {
                    weightMap[conditionId] = weight;
                    totalWeight += weight;
                }
            }

            // 전달받은 조건들을 직접 표시
            for (int i = 0; i < conditionItems.Length && i < allConditions.Count; i++)
            {
                var conditionItem = conditionItems[i];
                if (conditionItem != null)
                {
                    var condition = allConditions[i];
                    conditionItem.gameObject.SetActive(true);

                    // 전투 조건인지 버프 조건인지에 따라 처리
                    if (condition is InfiniteTowerBattleCondition battleCondition)
                    {
                        conditionItem.SetBattleCondition(battleCondition);
                    }
                    else if (condition is InfiniteTowerCondition buffCondition)
                    {
                        bool isGuaranteed = (i == 0 && floorData?.GuaranteedConditionId > 0);

                        // Weight 비율 계산 (랜덤 조건에만 적용)
                        float? weightPercentage = null;
                        if (!isGuaranteed && weightMap.TryGetValue(buffCondition.Id, out var weight) && totalWeight > 0)
                        {
                            weightPercentage = (float)weight / totalWeight * 100f;
                        }

                        conditionItem.SetCondition(buffCondition, isGuaranteed, weightPercentage);
                    }
                }
            }
        }
    }
}
