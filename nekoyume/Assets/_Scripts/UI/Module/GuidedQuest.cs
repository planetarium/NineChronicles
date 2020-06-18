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
            public readonly ReactiveProperty<WorldQuest> worldQuest =
                new ReactiveProperty<WorldQuest>();

            public readonly ReactiveProperty<CombinationEquipmentQuest> combinationEquipmentQuest =
                new ReactiveProperty<CombinationEquipmentQuest>();
        }

        private readonly ViewModel _viewModel = new ViewModel();

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();

        private GuidedQuestCell WorldQuestCell => cells[0];

        private GuidedQuestCell CombinationEquipmentQuestCell => cells[1];

        private void Awake()
        {
            Assert.AreEqual(cells.Count, 2);

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

            _viewModel.worldQuest.Value = questList
                .OfType<WorldQuest>()
                .FirstOrDefault(quest => !quest.Complete);

            _viewModel.combinationEquipmentQuest.Value = questList
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(quest => !quest.Complete);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SubscribeWorldQuest(WorldQuest worldQuest)
        {
            if (worldQuest is null)
            {
                WorldQuestCell.Hide();
            }
            else
            {
                WorldQuestCell.Show(worldQuest);
            }
        }

        private void SubscribeCombinationEquipmentQuest(
            CombinationEquipmentQuest combinationEquipmentQuest)
        {
            if (combinationEquipmentQuest is null)
            {
                CombinationEquipmentQuestCell.Hide();
            }
            else
            {
                CombinationEquipmentQuestCell.Show(combinationEquipmentQuest);
            }
        }
    }
}
