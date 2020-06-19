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
        public enum ViewState
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

        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        [SerializeField]
        private AnchoredPositionXTweener showAndHideTweener = null;

        private readonly ViewModel _viewModel = new ViewModel();
        private Tweener _showingAndHidingTweener;

        private readonly ISubject<(GuidedQuestCell cell, WorldQuest quest)>
            _onClickWorldQuestCell =
                new Subject<(GuidedQuestCell cell, WorldQuest quest)>();

        private readonly ISubject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>
            _onClickCombinationEquipmentQuestCell =
                new Subject<(GuidedQuestCell cell, CombinationEquipmentQuest quest)>();

        private GuidedQuestCell WorldQuestCell => cells[0];

        private GuidedQuestCell CombinationEquipmentQuestCell => cells[1];

        public ViewState State => _viewModel.state.Value;

        #region Events

        public IObservable<ViewState> OnStateChange => _viewModel.state;

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
            _showingAndHidingTweener?.Kill();
            _showingAndHidingTweener = null;
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

            switch (_viewModel.state.Value)
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
                    StartCoroutine(CoUpdateAvatarState(avatarState, null));
                    break;
            }
        }

        /// <summary>
        /// 스테이지 전투 종료 후 결과창이 뜨기 전에 호출합니다.
        /// 현재 노출된 스테이지 가이드 퀘스트 정보와 같은 스테이지일 경우에 동작합니다.
        /// </summary>
        /// <param name="stageId"></param>
        public void ClearWorldQuest(int stageId)
        {
            if (_viewModel.state.Value != ViewState.Shown)
            {
                Debug.LogWarning(
                    $"[{nameof(GuidedQuest)}] Cannot proceed because ViewState is {_viewModel.state}. Try when state is {ViewState.Shown}");
                return;
            }

            if (stageId != _viewModel.worldQuest.Value.Goal)
            {
                return;
            }

            EnterToClearExistGuidedQuest(_viewModel.worldQuest);
        }

        /// <summary>
        /// 메인 메뉴에 진입 후에 Shown 상태가 되었을 때와.. 호출합니다.
        /// 현재 노출된 장비 조합 가이드 퀘스트 정보와 같은 레시피일 경우에 동작합니다.
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="subRecipeId"></param>
        public void ClearCombinationEquipmentQuest(
            int recipeId,
            int? subRecipeId)
        {
            if (_viewModel.state.Value != ViewState.Shown)
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

            EnterToClearExistGuidedQuest(_viewModel.combinationEquipmentQuest);
        }

        public void Hide(bool ignoreAnimation = false)
        {
            EnterToHiding(ignoreAnimation);
        }

        #endregion

        #region ViewState

        private void EnterToShowing(AvatarState avatarState, bool ignoreAnimation = false)
        {
            _viewModel.state.Value = ViewState.None;
            _viewModel.worldQuest.Value = null;
            _viewModel.combinationEquipmentQuest.Value = null;
            _viewModel.state.Value = ViewState.Showing;

            if (ignoreAnimation)
            {
                gameObject.SetActive(true);
                StartCoroutine(CoUpdateAvatarState(avatarState, EnterToShown));
                return;
            }

            _showingAndHidingTweener?.Kill();
            _showingAndHidingTweener = showAndHideTweener
                .StartShowTween()
                .OnPlay(() => gameObject.SetActive(true))
                .OnComplete(() =>
                {
                    _showingAndHidingTweener = null;
                    StartCoroutine(CoUpdateAvatarState(avatarState, EnterToShown));
                });
        }

        private void EnterToShown()
        {
            _viewModel.state.Value = ViewState.Shown;
        }

        private IEnumerator CoUpdateAvatarState(AvatarState avatarState, System.Action onComplete)
        {
            var isAvatarStateChange = !avatarState.address.Equals(_viewModel.avatarAddress);
            _viewModel.avatarAddress = avatarState.address;

            var questList = avatarState.questList;
            var newWorldQuest = GetTargetWorldQuest(questList);
            var currentWorldQuest = _viewModel.worldQuest.Value;
            if (TryAddNewGuidedQuest(
                _viewModel.worldQuest,
                currentWorldQuest,
                newWorldQuest) &&
                !isAvatarStateChange)
            {
                yield return new WaitForSeconds(.5f);
            }

            var newCombinationEquipmentQuest = GetTargetCombinationEquipmentQuest(questList);
            var currentCombinationEquipmentQuest = _viewModel.combinationEquipmentQuest.Value;
            if (TryAddNewGuidedQuest(
                _viewModel.combinationEquipmentQuest,
                currentCombinationEquipmentQuest,
                newCombinationEquipmentQuest) &&
                !isAvatarStateChange)
            {
                yield return new WaitForSeconds(.5f);
            }

            onComplete?.Invoke();
        }

        private bool TryAddNewGuidedQuest<TQuestModel>(
            IReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel currentQuest,
            TQuestModel newQuest)
            where TQuestModel : Nekoyume.Model.Quest.Quest
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
                    EnterToAddNewGuidedQuest(questReactiveProperty, newQuest);
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

        private void EnterToAddNewGuidedQuest<TQuestModel>(
            IReactiveProperty<TQuestModel> questReactiveProperty,
            TQuestModel quest)
            where TQuestModel : Nekoyume.Model.Quest.Quest
        {
            _viewModel.state.Value = ViewState.AddNewGuidedQuest;
            questReactiveProperty.Value = quest;
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

            _showingAndHidingTweener?.Kill();
            _showingAndHidingTweener = showAndHideTweener
                .StartHideTween()
                .OnComplete(() =>
                {
                    _showingAndHidingTweener = null;
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
                if (state == ViewState.ClearExistGuidedQuest)
                {
                    // TODO: 완료하는 연출!
                    WorldQuestCell.Hide();
                }
                else
                {
                    WorldQuestCell.Hide(true);
                }
            }
            else
            {
                if (state == ViewState.Showing ||
                    state == ViewState.Shown ||
                    state == ViewState.AddNewGuidedQuest)
                {
                    // TODO: 더하는 연출!
                    WorldQuestCell.Show(worldQuest);
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
                if (state == ViewState.ClearExistGuidedQuest)
                {
                    // TODO: 완료하는 연출!
                    CombinationEquipmentQuestCell.Hide();
                }
                else
                {
                    CombinationEquipmentQuestCell.Hide(true);
                }
            }
            else
            {
                if (state == ViewState.Showing ||
                    state == ViewState.Shown ||
                    state == ViewState.AddNewGuidedQuest)
                {
                    // TODO: 더하는 연출!
                    CombinationEquipmentQuestCell.Show(combinationEquipmentQuest);
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
