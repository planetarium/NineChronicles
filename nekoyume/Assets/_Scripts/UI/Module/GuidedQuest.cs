using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
            public readonly ReactiveProperty<ViewState> state =
                new ReactiveProperty<ViewState>(ViewState.None);

            public readonly ReactiveProperty<WorldQuest> worldQuest =
                new ReactiveProperty<WorldQuest>();

            public readonly ReactiveProperty<CombinationEquipmentQuest> combinationEquipmentQuest =
                new ReactiveProperty<CombinationEquipmentQuest>();
        }

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        [SerializeField]
        private AnchoredPositionXTweener showAndHideTweener = null;

        private readonly ViewModel _viewModel = new ViewModel();

        #region Subjects

        private readonly ISubject<(GuidedQuestCell cell, WorldQuest quest)>
            _onClickWorldQuestCell =
                new Subject<(GuidedQuestCell cell, WorldQuest quest)>();

        private readonly ISubject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>
            _onClickCombinationEquipmentQuestCell =
                new Subject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>();

        private readonly ISubject<WorldQuest>
            _onClearWorldQuestComplete = new Subject<WorldQuest>();

        private readonly ISubject<CombinationEquipmentQuest>
            _onClearCombinationEquipmentQuestComplete = new Subject<CombinationEquipmentQuest>();

        #endregion

        private ViewState State => _viewModel.state.Value;

        public WorldQuest WorldQuest => _viewModel.worldQuest.Value;

        private GuidedQuestCell WorldQuestCell => cells[0];

        public CombinationEquipmentQuest CombinationEquipmentQuest =>
            _viewModel.combinationEquipmentQuest.Value;

        private GuidedQuestCell CombinationEquipmentQuestCell => cells[1];

        #region Events

        public IObservable<(GuidedQuestCell cell, WorldQuest quest)>
            OnClickWorldQuestCell => _onClickWorldQuestCell;

        public IObservable<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>
            OnClickCombinationEquipmentQuestCell => _onClickCombinationEquipmentQuestCell;

        #endregion

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
                .Subscribe(_onClickWorldQuestCell)
                .AddTo(gameObject);
            CombinationEquipmentQuestCell.onClick
                .Select(cell => (cell, _viewModel.combinationEquipmentQuest.Value))
                .Subscribe(_onClickCombinationEquipmentQuestCell)
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            showAndHideTweener.KillTween();
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

            switch (State)
            {
                default:
                    Debug.LogWarning(
                        $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.None}, {ViewState.Hidden} or {ViewState.Shown}");
                    break;
                case ViewState.None:
                case ViewState.Hidden:
                    EnterToShowing(avatarState, ignoreAnimation);
                    break;
                case ViewState.Shown:
                    StartCoroutine(CoUpdateAvatarState(avatarState, null));
                    break;
            }
        }

        public void Hide(bool ignoreAnimation = false)
        {
            EnterToHiding(ignoreAnimation);
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
            if (State != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Shown}");
                onComplete?.Invoke(false);
                return;
            }

            if (stageId != _viewModel.worldQuest.Value.Goal)
            {
                onComplete?.Invoke(false);
                return;
            }

            // NOTE: 이 라인까지 로직이 흐르면 `EnterToClearExistGuidedQuest()` 호출을 통해서
            // `_onClearWorldQuestComplete`가 반드시 호출되는 것을 기대합니다.
            _onClearWorldQuestComplete
                .Where(quest => quest.Goal == stageId)
                .First()
                .Subscribe(_ => onComplete?.Invoke(true));

            EnterToClearExistGuidedQuest(_viewModel.worldQuest);
        }

        /// <summary>
        /// 메인 메뉴에 진입 후에 Shown 상태가 되었을 때와.. 호출합니다.
        /// 현재 노출된 장비 조합 가이드 퀘스트 정보와 같은 레시피일 경우에 동작합니다.
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="subRecipeId"></param>
        /// <param name="onComplete">함께 전달 받은 `recipeId`와 `subRecipeId` 인자가 현재 노출된 장비 조합 가이드 퀘스트와 같다면 보상 연출이
        /// 끝난 후에 `true` 인자와 함께 `onComplete`가 호출됩니다. 그렇지 않다면 `false` 인자와 함께 호출됩니다.</param>
        public void ClearCombinationEquipmentQuest(
            int recipeId,
            int? subRecipeId,
            Action<bool> onComplete)
        {
            if (State != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Shown}");
                return;
            }

            if (recipeId != _viewModel.combinationEquipmentQuest.Value.RecipeId ||
                subRecipeId != _viewModel.combinationEquipmentQuest.Value.SubRecipeId)
            {
                return;
            }

            // NOTE: 이 라인까지 로직이 흐르면 `EnterToClearExistGuidedQuest()` 호출을 통해서
            // `_onClearCombinationEquipmentQuestComplete`가 반드시 호출되는 것을 기대합니다.
            _onClearCombinationEquipmentQuestComplete
                .Where(quest => quest.RecipeId == recipeId &&
                                quest.SubRecipeId == subRecipeId)
                .First()
                .Subscribe(_ => onComplete?.Invoke(true));

            EnterToClearExistGuidedQuest(_viewModel.combinationEquipmentQuest);
        }

        #endregion

        #region ViewState

        private void EnterToShowing(AvatarState avatarState, bool ignoreAnimation = false)
        {
            _viewModel.state.Value = ViewState.Showing;
            WorldQuestCell.Hide(true);
            CombinationEquipmentQuestCell.Hide(true);

            if (ignoreAnimation)
            {
                gameObject.SetActive(true);
                StartCoroutine(CoUpdateAvatarState(avatarState, EnterToShown));
                return;
            }

            showAndHideTweener
                .StartShowTween()
                .OnPlay(() => gameObject.SetActive(true))
                .OnComplete(() => StartCoroutine(CoUpdateAvatarState(avatarState, EnterToShown)));
        }

        private void EnterToShown()
        {
            _viewModel.state.Value = ViewState.Shown;
        }

        private IEnumerator CoUpdateAvatarState(AvatarState avatarState, System.Action onComplete)
        {
            var questList = avatarState.questList;
            var newWorldQuest = GetTargetWorldQuest(questList);
            var currentWorldQuest = _viewModel.worldQuest.Value;
            if (TryAddNewGuidedQuest(
                _viewModel.worldQuest,
                currentWorldQuest,
                newWorldQuest,
                WorldQuestCell))
            {
                yield return new WaitForSeconds(.5f);
            }

            var newCombinationEquipmentQuest = GetTargetCombinationEquipmentQuest(questList);
            var currentCombinationEquipmentQuest = _viewModel.combinationEquipmentQuest.Value;
            if (TryAddNewGuidedQuest(
                _viewModel.combinationEquipmentQuest,
                currentCombinationEquipmentQuest,
                newCombinationEquipmentQuest,
                CombinationEquipmentQuestCell))
            {
                yield return new WaitForSeconds(.5f);
            }

            onComplete?.Invoke();
        }

        private bool TryAddNewGuidedQuest<TQuestModel>(
            ReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel currentQuest,
            TQuestModel newQuest,
            GuidedQuestCell cell)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            if (newQuest is null)
            {
                if (currentQuest is null)
                {
                    return false;
                }

                // NOTE: 값이 비워지는 경우입니다. 이는 ClearExistGuidedQuest 상태로 처리되어야 합니다.
                Debug.LogError(
                    $"Clearing guided quest must proceed in {ViewState.ClearExistGuidedQuest} state.");
                return false;
            }

            if (currentQuest is null)
            {
                EnterToAddNewGuidedQuest(questReactiveProperty, newQuest);
                return true;
            }

            if (!currentQuest.Id.Equals(newQuest.Id))
            {
                // NOTE: 값이 바뀌는 경우입니다. 이는 ClearExistGuidedQuest 상태를 거치지 않았다는 말입니다.
                Debug.LogError(
                    $"Clearing exist guided quest first before add new guided quest.");
                return false;
            }

            if (!(cell.Quest is null))
            {
                return false;
            }

            // NOTE: 연출을 위해서 강제로 cell.Hide()를 호출했던 경우에 다시 보여주도록 합니다.
            EnterToAddNewGuidedQuest(questReactiveProperty, newQuest);
            return true;
        }

        private void EnterToAddNewGuidedQuest<TQuestModel>(
            ReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel quest)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            _viewModel.state.Value = ViewState.AddNewGuidedQuest;
            questReactiveProperty.SetValueAndForceNotify(quest);
        }

        private void EnterToClearExistGuidedQuest<TQuestModel>(
            IReactiveProperty<TQuestModel> questReactiveProperty)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            _viewModel.state.Value = ViewState.ClearExistGuidedQuest;
            questReactiveProperty.Value = null;
        }

        private void EnterToHiding(bool ignoreAnimation)
        {
            _viewModel.state.Value = ViewState.Hiding;

            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                EnterToHidden();
                return;
            }

            showAndHideTweener
                .StartHideTween()
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    EnterToHidden();
                });
        }

        private void EnterToHidden()
        {
            _viewModel.state.Value = ViewState.Hidden;
        }

        #endregion

        #region Getter

        private static WorldQuest GetTargetWorldQuest(QuestList questList) => questList?
            .OfType<WorldQuest>()
            .OrderBy(quest => quest.Goal)
            .FirstOrDefault(quest => !quest.Complete);

        private static CombinationEquipmentQuest GetTargetCombinationEquipmentQuest(
            QuestList questList) =>
            questList?
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(quest => !quest.Complete);

        #endregion

        #region Subscribe

        private void SubscribeWorldQuest(WorldQuest worldQuest)
        {
            var state = _viewModel.state.Value;
            if (worldQuest is null)
            {
                if (state == ViewState.ClearExistGuidedQuest &&
                    WorldQuestCell.Quest is WorldQuest quest)
                {
                    // TODO: 완료하는 연출!
                    WorldQuestCell.Hide();
                    _onClearWorldQuestComplete.OnNext(quest);
                    EnterToShown();
                }
                else
                {
                    WorldQuestCell.Hide(true);
                }
            }
            else
            {
                if (state == ViewState.AddNewGuidedQuest)
                {
                    WorldQuestCell.Show(worldQuest);
                    EnterToShown();
                }
                else
                {
                    WorldQuestCell.Show(worldQuest, true);
                }
            }
        }

        private void SubscribeCombinationEquipmentQuest(
            CombinationEquipmentQuest combinationEquipmentQuest)
        {
            var state = _viewModel.state.Value;
            if (combinationEquipmentQuest is null)
            {
                if (state == ViewState.ClearExistGuidedQuest &&
                    CombinationEquipmentQuestCell.Quest is CombinationEquipmentQuest quest)
                {
                    // TODO: 완료하는 연출!
                    CombinationEquipmentQuestCell.Hide();
                    _onClearCombinationEquipmentQuestComplete.OnNext(quest);
                    EnterToShown();
                }
                else
                {
                    CombinationEquipmentQuestCell.Hide(true);
                }
            }
            else
            {
                if (state == ViewState.AddNewGuidedQuest)
                {
                    CombinationEquipmentQuestCell.Show(combinationEquipmentQuest);
                    EnterToShown();
                }
                else
                {
                    CombinationEquipmentQuestCell.Show(combinationEquipmentQuest, true);
                }
            }
        }

        #endregion
    }
}
