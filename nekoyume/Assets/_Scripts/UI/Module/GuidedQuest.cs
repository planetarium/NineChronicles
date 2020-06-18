using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GuidedQuest : MonoBehaviour
    {
        private class ViewModel
        {
            public readonly ISubject<WorldQuest> worldQuest = new Subject<WorldQuest>();

            public readonly ISubject<CombinationEquipmentQuest> combinationEquipmentQuest =
                new Subject<CombinationEquipmentQuest>();
        }

        private readonly ViewModel _viewModel = new ViewModel();

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();
        
        private void Awake()
        {
            Assert.GreaterOrEqual(cells.Count, 2);

            _viewModel.worldQuest
                .Subscribe(SubscribeWorldQuest)
                .AddTo(gameObject);
            _viewModel.combinationEquipmentQuest
                .Subscribe(SubscribeCombinationEquipmentQuest)
                .AddTo(gameObject);

            foreach (var cell in cells)
            {
                cell.onClick.Subscribe(onClick).AddTo(gameObject);
            }
        }

        public void Show(bool ignoreAnimation = false)
        {
            var questList = States.Instance.CurrentAvatarState?.questList;
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

            var combinationEquipmentQuest = questList
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(quest => !quest.Complete);
            if (!(combinationEquipmentQuest is null))
            {
                cells[cellIndex++].Show(combinationEquipmentQuest, ignoreAnimation);
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

        private void SubscribeWorldQuest(WorldQuest worldQuest)
        {
        }

        private void SubscribeCombinationEquipmentQuest(
            CombinationEquipmentQuest combinationEquipmentQuest)
        {
        }
    }
}
