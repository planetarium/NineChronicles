using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        private Button goToCollectionButton;

        private void Awake()
        {
            goToCollectionButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                // shortcut to Collection
            });
        }

        public void Set(List<CollectionModel> models, bool displayEmpty = false)
        {
            var activeCount = models.Count(model => model.Active);
            contentContainer.SetActive(activeCount > 0);
            emptyContainer.SetActive(activeCount == 0 && displayEmpty);

            foreach (var view in statViews)
            {
                view.Hide();
            }

            var data = models
                .Where(model => model.Active)
                .SelectMany(model => model.Row.StatModifiers)
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
            maxCountText.text = $"/{models.Count}";
        }
    }
}
