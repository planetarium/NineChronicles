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
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    // TODO: 위젯으로 빼서 정적으로 동작할 수 있게 만드는 것이 좋았겠습니다.
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
            public AvatarState avatarState;

            public readonly ReactiveProperty<WorldQuest> worldQuest =
                new ReactiveProperty<WorldQuest>();

            public readonly ReactiveProperty<CombinationEquipmentQuest> combinationEquipmentQuest =
                new ReactiveProperty<CombinationEquipmentQuest>();
        }

        private static readonly ViewModel SharedViewModel = new ViewModel();

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        [SerializeField]
        private AnchoredPositionXTweener showingAndHidingTweener = null;

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

        private readonly ReactiveProperty<ViewState> _state =
            new ReactiveProperty<ViewState>(ViewState.None);

        public static WorldQuest WorldQuest => SharedViewModel.worldQuest.Value;

        private GuidedQuestCell WorldQuestCell => cells[0];

        public static CombinationEquipmentQuest CombinationEquipmentQuest =>
            SharedViewModel.combinationEquipmentQuest.Value;

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
            SharedViewModel.worldQuest
                .Subscribe(SubscribeWorldQuest)
                .AddTo(gameObject);
            SharedViewModel.combinationEquipmentQuest
                .Subscribe(SubscribeCombinationEquipmentQuest)
                .AddTo(gameObject);

            // 뷰 오브젝트를 구독합니다.
            WorldQuestCell.onClick
                .Select(cell => (cell, SharedViewModel.worldQuest.Value))
                .Subscribe(_onClickWorldQuestCell)
                .AddTo(gameObject);
            CombinationEquipmentQuestCell.onClick
                .Select(cell => (cell, SharedViewModel.combinationEquipmentQuest.Value))
                .Subscribe(_onClickCombinationEquipmentQuestCell)
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            showingAndHidingTweener.KillTween();
        }

        #endregion

        #region Control

        /// <summary>
        /// GuidedQuest를 초기화하면서 노출시킵니다.
        /// 이미 초기화되어 있다면 `avatarState` 인자에 따라 재초기화를 하거나 새로운 가이드 퀘스트를 더하는 AddNewGuidedQuest 상태로 진입합니다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <param name="ignoreAnimation"></param>
        public void Show(AvatarState avatarState, System.Action onComplete = null, bool ignoreAnimation = false)
        {
            if (avatarState is null)
            {
                SharedViewModel.avatarState = null;
                SharedViewModel.worldQuest.Value = null;
                SharedViewModel.combinationEquipmentQuest.Value = null;
                onComplete?.Invoke();
                return;
            }

            SharedViewModel.avatarState = avatarState;

            switch (_state.Value)
            {
                default:
                    Debug.LogWarning(
                        $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_state.Value}. Try when state is {ViewState.None}, {ViewState.Hidden} or {ViewState.Shown}");
                    break;
                case ViewState.None:
                case ViewState.Hidden:
                    EnterToShowing(onComplete, ignoreAnimation);
                    break;
                case ViewState.Shown:
                    StartCoroutine(CoUpdateList(onComplete));
                    break;
            }
        }

        public void Hide(bool ignoreAnimation = false)
        {
            EnterToHiding(ignoreAnimation);
        }

        public void SetWorldQuestToInProgress(int stageId)
        {
            if (SharedViewModel.worldQuest.Value?.Goal != stageId)
            {
                return;
            }

            WorldQuestCell.SetToInProgress(true);
        }

        public void SetCombinationEquipmentToInProgress(int recipeId)
        {
            if (SharedViewModel.combinationEquipmentQuest.Value?.RecipeId != recipeId)
            {
                return;
            }

            CombinationEquipmentQuestCell.SetToInProgress(true);
        }

        /// <summary>
        /// 현재 노출된 스테이지 가이드 퀘스트 정보와 같은 스테이지일 경우에 동작합니다.
        /// 클리어 처리가 될 때에는 `QuestResult`를 띄우는 것을 포함하는 연출을 책임집니다.
        /// </summary>
        /// <param name="stageId"></param>
        /// <param name="onComplete">함께 전달 받은 `stageId` 인자가 현재 노출된 스테이지 가이드 퀘스트와 같다면 보상 연출이
        /// 끝난 후에 `true` 인자와 함께 `onComplete`가 호출됩니다. 그렇지 않다면 `false` 인자와 함께 호출됩니다.</param>
        public void ClearWorldQuest(int stageId, Action<bool> onComplete)
        {
            if (_state.Value != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_state.Value}. Try when state is {ViewState.Shown}");
                onComplete?.Invoke(false);
                return;
            }

            if (SharedViewModel.worldQuest.Value is null ||
                stageId != SharedViewModel.worldQuest.Value.Goal)
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

            EnterToClearExistGuidedQuest(SharedViewModel.worldQuest);
        }

        /// <summary>
        /// 현재 노출된 장비 조합 가이드 퀘스트 정보와 같은 레시피일 경우에 동작합니다.
        /// `ClearWorldQuest`와는 다르게 `QuestResult`를 띄우지 않습니다.
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="onComplete">함께 전달 받은 `recipeId`와 `subRecipeId` 인자가 현재 노출된 장비 조합 가이드 퀘스트와 같다면 보상 연출이
        /// 끝난 후에 `true` 인자와 함께 `onComplete`가 호출됩니다. 그렇지 않다면 `false` 인자와 함께 호출됩니다.</param>
        public void ClearCombinationEquipmentQuest(
            int recipeId,
            Action<bool> onComplete)
        {
            if (_state.Value != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_state.Value}. Try when state is {ViewState.Shown}");
                return;
            }

            if (recipeId != SharedViewModel.combinationEquipmentQuest.Value.RecipeId)
            {
                return;
            }

            // NOTE: 이 라인까지 로직이 흐르면 `EnterToClearExistGuidedQuest()` 호출을 통해서
            // `_onClearCombinationEquipmentQuestComplete`가 반드시 호출되는 것을 기대합니다.
            _onClearCombinationEquipmentQuestComplete
                .Where(quest => quest.RecipeId == recipeId)
                .First()
                .Subscribe(_ => onComplete?.Invoke(true));

            EnterToClearExistGuidedQuest(SharedViewModel.combinationEquipmentQuest);
        }

        /// <summary>
        /// SharedViewModel.avatarState를 사용해서 리스트를 업데이트 합니다.
        /// </summary>
        /// <param name="onComplete"></param>
        public void UpdateList(System.Action onComplete = null)
        {
            UpdateList(SharedViewModel.avatarState, onComplete);
        }

        /// <summary>
        /// avatarState 인자를 사용해서 리스트를 업데이트 합니다.
        /// </summary>
        /// <param name="avatarState"></param>
        /// <param name="onComplete"></param>
        public void UpdateList(AvatarState avatarState, System.Action onComplete = null)
        {
            if (_state.Value != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_state.Value}. Try when state is {ViewState.Shown}");
                return;
            }

            SharedViewModel.avatarState = avatarState;
            StartCoroutine(CoUpdateList(onComplete));
        }

        #endregion

        #region ViewState

        private void EnterToShowing(System.Action onExit = null, bool ignoreAnimation = false)
        {
            _state.Value = ViewState.Showing;

            // NOTE: SharedViewModel.worldQuest.Value에 null을 넣지 않고 WorldQuestCell.Hide()를 호출합니다.
            // 이는 뷰 모델과 상관없이 연출을 위해서 뷰 오브젝트만 숨기기 위해서 입니다.
            WorldQuestCell.Hide();
            CombinationEquipmentQuestCell.Hide();

            if (ignoreAnimation)
            {
                gameObject.SetActive(true);
                StartCoroutine(CoUpdateList(() =>
                {
                    onExit?.Invoke();
                    EnterToShown();
                }));
                return;
            }

            showingAndHidingTweener
                .PlayTween()
                .OnPlay(() => gameObject.SetActive(true))
                .OnComplete(() => StartCoroutine(CoUpdateList(() =>
                {
                    onExit?.Invoke();
                    EnterToShown();
                })));
        }

        private void EnterToShown()
        {
            _state.Value = ViewState.Shown;
        }

        private IEnumerator CoUpdateList(System.Action onComplete)
        {
            var questList = SharedViewModel.avatarState?.questList;
            var newWorldQuest = GetTargetWorldQuest(questList);
            if (TryEnterToAddNewGuidedQuest(
                SharedViewModel.worldQuest,
                newWorldQuest,
                !(WorldQuestCell.Quest is null)))
            {
                yield return new WaitUntil(() => _state.Value == ViewState.Shown);
            }

            var newCombinationEquipmentQuest = GetTargetCombinationEquipmentQuest(questList);
            if (TryEnterToAddNewGuidedQuest(
                SharedViewModel.combinationEquipmentQuest,
                newCombinationEquipmentQuest,
                !(CombinationEquipmentQuestCell.Quest is null)))
            {
                yield return new WaitUntil(() => _state.Value == ViewState.Shown);
            }

            onComplete?.Invoke();
        }

        private bool TryEnterToAddNewGuidedQuest<TQuestModel>(
            ReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel newQuest,
            bool cellHasQuest)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            var currentQuest = questReactiveProperty.Value;

            if (newQuest is null)
            {
                if (currentQuest is null)
                {
                    return false;
                }

                // NOTE: ClearExistGuidedQuest 상태로 처리되지 않았는데, 셀이 비워져야 하는 상태입니다.
                // 이때에는 해당 프로퍼티에 null을 할당해서 셀이 아무런 연출 없이 Hide() 되도록 합니다.
                questReactiveProperty.Value = null;
                return false;
            }

            if (currentQuest is null)
            {
                EnterToAddNewGuidedQuest(questReactiveProperty, newQuest);
                return true;
            }


            if (currentQuest.Id.Equals(newQuest.Id))
            {
                if (cellHasQuest)
                {
                    return false;
                }

                // NOTE: 연출을 위해서 강제로 cell.Hide()를 호출했던 경우에는 뷰 모델인 currentQuest의 값과는 상관 없이
                // 뷰 오브젝트인 GuidedQuestCell에서 도출된 cellHasQuest가 false가 됩니다.
                // cellHasQuest가 false일 때에는 다시 보여주도록 EnterToAddNewGuidedQuest를 호출하고 true를 반환해야 합니다.
                // 따라서 이후 라인으로 그대로 흐르게 둡니다.
            }

            EnterToAddNewGuidedQuest(questReactiveProperty, newQuest);
            return true;
        }

        private void EnterToAddNewGuidedQuest<TQuestModel>(
            ReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel quest)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            _state.Value = ViewState.AddNewGuidedQuest;
            questReactiveProperty.SetValueAndForceNotify(quest);
        }

        private void EnterToClearExistGuidedQuest<TQuestModel>(
            IReactiveProperty<TQuestModel> questReactiveProperty)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            _state.Value = ViewState.ClearExistGuidedQuest;
            questReactiveProperty.Value = null;
        }

        private void EnterToHiding(bool ignoreAnimation)
        {
            _state.Value = ViewState.Hiding;

            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                EnterToHidden();
                return;
            }

            showingAndHidingTweener
                .PlayReverse()
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    EnterToHidden();
                });
        }

        private void EnterToHidden()
        {
            _state.Value = ViewState.Hidden;
        }

        #endregion

        #region Getter

        private static WorldQuest GetTargetWorldQuest(QuestList questList)
        {
            #pragma warning disable 0162
            if (GameConfig.RequireClearedStageLevel.UIMainMenuStage > 0)
            {
                if (SharedViewModel.avatarState is null ||
                    !SharedViewModel.avatarState.worldInformation.TryGetLastClearedStageId(
                        out var lastClearedStageId) ||
                    lastClearedStageId < GameConfig.RequireClearedStageLevel.UIMainMenuStage)
                {
                    return null;
                }
            }
            #pragma warning restore 0162

            var targetQuest = questList?
                .OfType<WorldQuest>()
                .Where(quest => !quest.Complete)
                .OrderBy(quest => quest.Goal)
                .FirstOrDefault();
            if (targetQuest is null)
            {
                return null;
            }

            var targetStageId = targetQuest.Goal;
            return !Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(targetStageId, out _)
                ? null
                : targetQuest;
        }

        private static CombinationEquipmentQuest GetTargetCombinationEquipmentQuest(
            QuestList questList)
        {
            if (SharedViewModel.avatarState is null ||
                !SharedViewModel.avatarState.worldInformation.TryGetLastClearedStageId(out var lastClearedStageId) ||
                lastClearedStageId < GameConfig.RequireClearedStageLevel.CombinationEquipmentAction)
            {
                return null;
            }

            return questList?
                .OfType<CombinationEquipmentQuest>()
                .Where(quest => !quest.Complete &&
                                quest.StageId <= lastClearedStageId)
                .OrderBy(quest => quest.StageId)
                .FirstOrDefault();
        }

        #endregion

        #region Subscribe

        private void SubscribeWorldQuest(WorldQuest worldQuest)
        {
            var state = _state.Value;
            if (worldQuest is null)
            {
                if (state == ViewState.ClearExistGuidedQuest &&
                    WorldQuestCell.Quest is WorldQuest quest)
                {
                    WorldQuestCell.HideAsClear(cell =>
                    {
                        EnterToShown();
                        _onClearWorldQuestComplete.OnNext(quest);
                    });
                }
                else
                {
                    WorldQuestCell.Hide();
                }
            }
            else
            {
                if (state == ViewState.AddNewGuidedQuest)
                {
                    WorldQuestCell.ShowAsNew(worldQuest, cell => EnterToShown());
                }
                else
                {
                    WorldQuestCell.Show(worldQuest);
                }
            }
        }

        private void SubscribeCombinationEquipmentQuest(
            CombinationEquipmentQuest combinationEquipmentQuest)
        {
            var state = _state.Value;
            if (combinationEquipmentQuest is null)
            {
                if (state == ViewState.ClearExistGuidedQuest &&
                    CombinationEquipmentQuestCell.Quest is CombinationEquipmentQuest quest)
                {
                    // TODO: 완료하는 연출!
                    CombinationEquipmentQuestCell.HideAsClear(cell =>
                    {
                        EnterToShown();
                        _onClearCombinationEquipmentQuestComplete.OnNext(quest);
                    });
                }
                else
                {
                    CombinationEquipmentQuestCell.Hide();
                }
            }
            else
            {
                if (state == ViewState.AddNewGuidedQuest)
                {
                    CombinationEquipmentQuestCell.ShowAsNew(
                        combinationEquipmentQuest,
                        cell => EnterToShown());
                }
                else
                {
                    CombinationEquipmentQuestCell.Show(combinationEquipmentQuest);
                }
            }
        }

        #endregion
    }
}
