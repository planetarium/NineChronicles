using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Libplanet;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.UI.Scroller;
using Nekoyume.UI.Tween;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    /// <summary>
    /// Show()를 통해 전달 받은 AvatarState의 퀘스트 리스트를 기반으로 가이드 퀘스트를 노출합니다.
    /// 가이드 퀘스트 목록에 새로운 것이 추가되거나 목록의 것이 완료될 때의 연출을 책임집니다.
    /// </summary>
    public class GuidedQuest : MonoBehaviour
    {
        private enum ViewState
        {
            /// <summary>
            /// 최초 상태입니다.
            /// </summary>
            None = -1,

            /// <summary>
            /// 보여지는 연출 상태입니다.
            /// </summary>
            Showing,

            /// <summary>
            /// 보여진 상태입니다. AvatarState가 설정되어 있는 유휴 상태이기도 합니다.
            /// </summary>
            Shown,

            /// <summary>
            /// 새로운 가이드 퀘스트를 더하는 연출 상태입니다.
            /// </summary>
            AddNewGuidedQuest,

            /// <summary>
            /// 기존 가이드 퀘스트를 완료하는 연출 상태입니다.
            /// 보상 연출을 포함합니다.
            /// </summary>
            ClearExistGuidedQuest,

            /// <summary>
            /// 사라지는 연출 상태입니다.
            /// </summary>
            Hiding,

            /// <summary>
            /// 사라진 상태입니다.
            /// </summary>
            Hidden,
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

        private class UnexpectedException : Exception
        {
            public UnexpectedException(string message) : base(message)
            {
            }
        }

        private readonly ViewModel _viewModel = new ViewModel();

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        [SerializeField]
        private AnchoredPositionXTweener showAndHideTweener = null;

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

            // 뷰 모델을 구독합니다.
            _viewModel.worldQuest
                .Subscribe(SubscribeWorldQuest)
                .AddTo(gameObject);
            _viewModel.combinationEquipmentQuest
                .Subscribe(SubscribeCombinationEquipmentQuest)
                .AddTo(gameObject);

            // 뷰 오브젝트를 구독합니다.
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

        /// <summary>
        /// GuidedQuest를 초기화하면서 노출시킵니다.
        /// 이미 초기화되어 있다면 `avatarState` 인자에 따라 재초기화를 하거나 새로운 가이드 퀘스트를 더하는 AddNewGuidedQuest 상태로 진입합니다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <param name="ignoreAnimation"></param>
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
                        $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.None} or {ViewState.Shown}");
                    break;
                case ViewState.None:
                case ViewState.Hidden:
                    EnterToShowing(avatarState, ignoreAnimation);
                    break;
                case ViewState.Shown:
                    StartCoroutine(CoUpdateAvatarState(avatarState, ignoreAnimation));
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
        public void ClearWorldQuest(int stageId, Action<bool> onComplete)
        {
            if (_viewModel.state != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Shown}");
                onComplete(false);
                return;
            }

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
            Action<bool> onComplete)
        {
            if (_viewModel.state != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Shown}");
                onComplete(false);
                return;
            }

            if (recipeId != _viewModel.combinationEquipmentQuest.Value.RecipeId ||
                subRecipeId != _viewModel.combinationEquipmentQuest.Value.SubRecipeId)
            {
                onComplete(false);
                return;
            }

            onComplete(true);
        }

        public void Hide(bool ignoreAnimation = false)
        {
            if (ignoreAnimation)
            {
                EnterToHidden();
            }
            else
            {
                EnterToHiding(ignoreAnimation);
            }
        }

        #endregion

        #region ViewState

        private Tweener _showAndHideTweener;

        private void EnterToShowing(AvatarState avatarState, bool ignoreAnimation = false)
        {
            _viewModel.state = ViewState.Showing;

            if (ignoreAnimation)
            {
                EnterToShown(avatarState);
                return;
            }

            _showAndHideTweener?.Kill();
            _showAndHideTweener = showAndHideTweener.StartShowTween();
            _showAndHideTweener.OnComplete(() => EnterToShown(avatarState));
        }

        private void EnterToShown(AvatarState avatarState)
        {
            _viewModel.state = ViewState.Shown;
            _showAndHideTweener = null;

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
        }

        private IEnumerator CoUpdateAvatarState(AvatarState avatarState, bool ignoreAnimation)
        {
            if (!avatarState.address.Equals(_viewModel.avatarAddress))
            {
                EnterToShown(avatarState);
                yield break;
            }

            var questList = avatarState.questList;
            var newWorldQuest = GetTargetWorldQuest(questList);
            var currentWorldQuest = _viewModel.worldQuest.Value;
            var isComplete = false;
            if (TryAddNewGuidedQuest(
                WorldQuestCell,
                currentWorldQuest,
                newWorldQuest,
                () => isComplete = true))
            {
                yield return new WaitUntil(() => isComplete);
                isComplete = false;
            }

            var newCombinationEquipmentQuest = GetTargetCombinationEquipmentQuest(questList);
            var currentCombinationEquipmentQuest = _viewModel.combinationEquipmentQuest.Value;
            if (TryAddNewGuidedQuest(
                CombinationEquipmentQuestCell,
                currentCombinationEquipmentQuest,
                newCombinationEquipmentQuest,
                () => isComplete = true))
            {
                yield return new WaitUntil(() => isComplete);
            }
        }

        private bool TryAddNewGuidedQuest(
            GuidedQuestCell cell,
            Nekoyume.Model.Quest.Quest currentQuest,
            Nekoyume.Model.Quest.Quest newQuest,
            System.Action onComplete)
        {
            if (newQuest is null)
            {
                if (!(currentQuest is null))
                {
                    // NOTE: 값이 비워지는 경우입니다. 이는 ClearExistGuidedQuest 상태로 처리되어야 합니다.
                    throw new UnexpectedException(
                        $"Clearing guided quest must proceed in {ViewState.ClearExistGuidedQuest} state.");
                }
            }
            else
            {
                if (currentQuest is null)
                {
                    EnterToAddNewGuidedQuest(cell, newQuest, onComplete);
                    return true;
                }

                if (!currentQuest.Id.Equals(newQuest.Id))
                {
                    // NOTE: 값이 바뀌는 경우입니다. 이는 ClearExistGuidedQuest 상태를 거치지 않았다는 말입니다.
                    throw new UnexpectedException(
                        $"Clearing exist guided quest first before add new guided quest.");
                }
            }

            return false;
        }

        private void EnterToAddNewGuidedQuest(
            GuidedQuestCell cell,
            Nekoyume.Model.Quest.Quest quest,
            System.Action onComplete)
        {
            _viewModel.state = ViewState.AddNewGuidedQuest;

            // TODO: 더하는 연출!
            cell.Show(quest);
            onComplete();
        }

        private void EnterToClearExistGuidedQuest()
        {
            _viewModel.state = ViewState.ClearExistGuidedQuest;

            // TODO: 완료하는 연출!
        }

        private void EnterToHiding(bool ignoreAnimation)
        {
            _viewModel.state = ViewState.Hiding;

            if (ignoreAnimation)
            {
                EnterToHidden();
                return;
            }

            _showAndHideTweener?.Kill();
            _showAndHideTweener = showAndHideTweener.StartHideTween();
            _showAndHideTweener.OnComplete(EnterToHidden);
        }

        private void EnterToHidden()
        {
            _viewModel.state = ViewState.Hidden;
            _showAndHideTweener = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region Getter

        private static WorldQuest GetTargetWorldQuest(QuestList questList) => questList
            .OfType<WorldQuest>()
            .FirstOrDefault(quest => !quest.Complete);

        private static CombinationEquipmentQuest GetTargetCombinationEquipmentQuest(
            QuestList questList) =>
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
