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

        public readonly ISubject<(GuidedQuestCell cell, WorldQuest quest)> onClickWorldQuestCell =
            new Subject<(GuidedQuestCell cell, WorldQuest quest)>();

        public readonly ISubject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>
            onClickCombinationEquipmentQuestCell =
                new Subject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>();

        private GuidedQuestCell WorldQuestCell => cells[0];

        private GuidedQuestCell CombinationEquipmentQuestCell => cells[1];

        private void Awake()
        {
            // NOTE: 지금은 딱 두 줄만 표시합니다.
            Assert.AreEqual(cells.Count, 2);

            _viewModel.worldQuest
                .Subscribe(SubscribeWorldQuest)
                .AddTo(gameObject);
            _viewModel.combinationEquipmentQuest
                .Subscribe(SubscribeCombinationEquipmentQuest)
                .AddTo(gameObject);

            WorldQuestCell.onClick
                .Select(cell => (cell, _viewModel.worldQuest.Value))
                .Subscribe(onClickWorldQuestCell)
                .AddTo(gameObject);
            CombinationEquipmentQuestCell.onClick
                .Select(cell => (cell, _viewModel.combinationEquipmentQuest.Value))
                .Subscribe(onClickCombinationEquipmentQuestCell)
                .AddTo(gameObject);
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

        /// <summary>
        /// 스테이지 전투 종료 후 결과창이 뜨기 전에 호출합니다.
        /// 현재 노출된 스테이지 가이드 퀘스트 정보와 같은 스테이지일 경우에 동작합니다.
        /// </summary>
        /// <param name="stageId"></param>
        /// <param name="onComplete">함께 전달 받은 `stageId` 인자가 현재 노출된 스테이지 가이드 퀘스트와 같다면 보상 연출이
        /// 끝난 후에 `true` 인자와 함께 `onComplete`가 호출됩니다. 그렇지 않다면 `false` 인자와 함께 호출됩니다.</param>
        public void ClearWorldQuest(int stageId, System.Action<bool> onComplete)
        {
            if (stageId != _viewModel.worldQuest.Value.Goal)
            {
                onComplete(false);
                return;
            }

            onComplete(true);
        }

        /// <summary>
        /// 메인 메뉴에 진입 후에 Shown 상태가 되었을 때와.. 호출합니다.
        /// 현재 노출된 장비 조합 가이드 퀘스트 정보와 같은 레시피일 경우에 동작합니다.
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="subRecipeId"></param>
        /// <param name="onComplete">함께 전달 받은 `stageId` 인자가 현재 노출된 스테이지 가이드 퀘스트와 같다면 보상 연출이
        /// 끝난 후에 `true` 인자와 함께 `onComplete`가 호출됩니다. 그렇지 않다면 `false` 인자와 함께 호출됩니다.</param>
        public void ClearCombinationEquipmentQuest(
            int recipeId,
            int? subRecipeId,
            System.Action<bool> onComplete)
        {
            if (recipeId != _viewModel.combinationEquipmentQuest.Value.RecipeId ||
                subRecipeId != _viewModel.combinationEquipmentQuest.Value.SubRecipeId)
            {
                onComplete(false);
                return;
            }

            onComplete(true);
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
