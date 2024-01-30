using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
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

        public void Set(Collection.Model[] models)
        {
            var activeCount = models.Count(model => model.Active);
            contentContainer.SetActive(activeCount > 0);
            emptyContainer.SetActive(activeCount == 0);

            foreach (var view in statViews)
            {
                view.Hide();
            }

            var data = models
                .Where(model => model.Active)
                .SelectMany(model => model.Row.StatModifiers)
                .GroupBy(stat => stat.StatType);

            var i = 0;
            foreach (var grouping in data)
            {
                var statType = grouping.Key;
                var percentageValue = grouping
                    .Where(stat => stat.Operation == StatModifier.OperationType.Percentage)
                    .Sum(s => s.Value);
                var addValue = grouping
                    .Where(stat => stat.Operation == StatModifier.OperationType.Add)
                    .Sum(s => s.Value);

                statViews[i].Show(statType,
                    StatModifier.OperationType.Add, addValue,
                    StatModifier.OperationType.Percentage, percentageValue);
                i++;
            }

            countText.text = activeCount.ToString();
            maxCountText.text = $"/{models.Length.ToString()}";
        }
    }
}
