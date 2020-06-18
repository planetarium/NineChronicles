using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Scroller;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GuidedQuest : MonoBehaviour
    {
        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();

        private void Awake()
        {
            Assert.GreaterOrEqual(cells.Count, 2);

            foreach (var cell in cells)
            {
                cell.onClick.Subscribe(onClick).AddTo(gameObject);
            }
        }

        public void Show(QuestList questList, bool ignoreAnimation = false)
        {
            if (questList is null)
            {
                return;
            }

            var cellIndex = 0;
            var worldQuest = questList
                .OfType<WorldQuest>()
                .FirstOrDefault(quest => !quest.Complete);
            if (!(worldQuest is null))
            {
                cells[cellIndex++].Show(worldQuest, ignoreAnimation);
            }

            for (var i = cellIndex; i < cells.Count; i++)
            {
                var cell = cells[cellIndex];
                cell.Hide();
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
