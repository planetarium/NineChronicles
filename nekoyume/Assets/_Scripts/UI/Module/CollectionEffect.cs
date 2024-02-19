using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CollectionEffect : MonoBehaviour
    {
        [SerializeField]
        private DetailedStatView[] statViews;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private TextMeshProUGUI maxCountText;

        [SerializeField]
        private GameObject contentContainer;

        [SerializeField]
        private GameObject emptyContainer;

        public void Set(int activeCount, int maxCount, IEnumerable<StatModifier> stats)
        {
            contentContainer.SetActive(activeCount > 0);
            emptyContainer.SetActive(activeCount == 0);

            foreach (var view in statViews)
            {
                view.Hide();
            }

            var data = stats
                .GroupBy(stat => stat.StatType)
                .Select(grouping => (
                    StatType: grouping.Key,
                    AddValue: grouping
                        .Where(stat => stat.Operation == StatModifier.OperationType.Add)
                        .Sum(stat => stat.Value),
                    PercentageValue: grouping
                        .Where(stat => stat.Operation == StatModifier.OperationType.Percentage)
                        .Sum(stat => stat.Value)
                ));

            var i = 0;
            foreach (var (statType, addValue, percentageValue) in data)
            {
                statViews[i++].ShowModify(statType, addValue, percentageValue);
            }

            countText.text = activeCount.ToString();
            maxCountText.text = $"/ {maxCount}";
        }
    }
}
