using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.UI.Scroller;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GuidedQuest : MonoBehaviour
    {
        private enum ViewState
        {
            /// <summary>
            /// 최초 상태입니다.
            /// </summary>
            None = -1,

            /// <summary>
            /// AvatarState가 설정되어 있는 유휴 상태입니다.
            /// </summary>
            Idle,

            /// <summary>
            /// 새로운 가이드 퀘스트를 더하는 연출 상태입니다.
            /// </summary>
            AddNewGuidedQuest,

            /// <summary>
            /// 기존 가이드 퀘스트를 완료하는 연출 상태입니다.
            /// 보상 연출을 포함합니다.
            /// </summary>
            ClearExistGuidedQuest,
        }

        private class ViewModel
        {
            public ViewState state = ViewState.None;

            public Address avatarAddress;

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

        #region MonoBehaviour

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

        #endregion

        #region Controll

        public void Show(AvatarState avatarState, bool ignoreAnimation = false)
        {
            if (avatarState is null)
            {
                return;
            }

            switch (_viewModel.state)
            {
                default:
                    Debug.LogWarning(
                        $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Idle}");
                    break;
                case ViewState.None:
                    Initialize(avatarState);
                    break;
                case ViewState.Idle:
                    UpdateAvatarState(avatarState, ignoreAnimation);
                    break;
            }

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

        #endregion

        #region ViewState

        private void Initialize(AvatarState avatarState)
        {
            var questList = avatarState.questList;
            if (questList is null)
            {
                _viewModel.worldQuest.Value = null;
                _viewModel.combinationEquipmentQuest.Value = null;
            }
            else
            {
                _viewModel.worldQuest.Value = GetTargetWorldQuest(questList);
                _viewModel.combinationEquipmentQuest.Value =
                    GetTargetCombinationEquipmentQuest(questList);
            }

            _viewModel.avatarAddress = avatarState.address;
            _viewModel.state = ViewState.Idle;
        }

        private void UpdateAvatarState(AvatarState avatarState, bool ignoreAnimation)
        {
            if (!avatarState.address.Equals(_viewModel.avatarAddress))
            {
                Initialize(avatarState);
                return;
            }

            var questList = avatarState.questList;
            var worldQuest = GetTargetWorldQuest(questList);
            var combinationEquipmentQuest = GetTargetCombinationEquipmentQuest(questList);
            // TODO: 퀘스트 변화가 있다면 뷰상태를 AddNewGuidedQuest나 ClearExistGuidedQuest로 전환합니다.
        }

        #endregion

        #region Getter

        private static WorldQuest GetTargetWorldQuest(QuestList questList) => questList
            .OfType<WorldQuest>()
            .FirstOrDefault(quest => !quest.Complete);

        private static CombinationEquipmentQuest GetTargetCombinationEquipmentQuest(QuestList questList) =>
            questList
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(quest => !quest.Complete);

        #endregion

        #region Subscribe

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

        #endregion
    }
}
