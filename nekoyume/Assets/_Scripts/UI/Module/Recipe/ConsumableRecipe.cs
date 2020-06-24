using System;
using System.Collections.Generic;
using Nekoyume.UI.Scroller;
using UniRx;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Tween;
using Nekoyume.Model.Stat;

namespace Nekoyume.UI.Module
{
    public class ConsumableRecipe : MonoBehaviour
    {
        [SerializeField]
        private ConsumableRecipeCellView cellViewPrefab;
        [SerializeField]
        private ConsumableRecipeCellView[] cellViews;
        [SerializeField]
        private TabButton hpTabButton;
        [SerializeField]
        private TabButton atkTabButton;
        [SerializeField]
        private TabButton criTabButton;
        [SerializeField]
        private TabButton hitTabButton;
        [SerializeField]
        private TabButton defTabButton;
        [SerializeField]
        private Transform cellViewParent;
        [SerializeField]
        private ScrollRect scrollRect;

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly ReactiveProperty<StatType> _filterType =
            new ReactiveProperty<StatType>(StatType.HP);

        private readonly List<IDisposable> _disposablesAtLoadRecipeList = new List<IDisposable>();

        public DOTweenGroupAlpha scrollAlphaTweener;
        public AnchoredPositionYTweener scrollPositionTweener;

        private void Awake()
        {
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(hpTabButton);
            _toggleGroup.RegisterToggleable(atkTabButton);
            _toggleGroup.RegisterToggleable(criTabButton);
            _toggleGroup.RegisterToggleable(hitTabButton);
            _toggleGroup.RegisterToggleable(defTabButton);

            LoadRecipes();
            _filterType.Subscribe(SubScribeFilterType).AddTo(gameObject);
        }

        private void OnEnable()
        {
            if (States.Instance.CurrentAvatarState is null)
                return;

            UpdateRecipes();
        }

        private void OnDestroy()
        {
            _filterType.Dispose();
            _disposablesAtLoadRecipeList.DisposeAllAndClear();
        }

        public void ShowCellViews()
        {
            scrollAlphaTweener.Play();
            scrollPositionTweener.StartTween();

            foreach (var view in cellViews)
            {
                view.SetInteractable(true);
            }
        }

        public void HideCellviews()
        {
            scrollAlphaTweener.PlayReverse();
            scrollPositionTweener.PlayReverse();
            foreach (var view in cellViews)
            {
                view.SetInteractable(false);
            }
        }

        private void LoadRecipes()
        {
            _disposablesAtLoadRecipeList.DisposeAllAndClear();

            var recipeSheet = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet;
            var totalCount = recipeSheet.Count;
            cellViews = new ConsumableRecipeCellView[totalCount];

            var idx = 0;
            foreach (var recipeRow in recipeSheet)
            {
                var cellView = Instantiate(cellViewPrefab, cellViewParent);
                cellView.Set(recipeRow);
                cellView.OnClick.Subscribe(SubscribeOnClickCellView).AddTo(_disposablesAtLoadRecipeList);
                cellViews[idx] = cellView;
                ++idx;
            }

            UpdateRecipes();
        }

        public void UpdateRecipes()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
                return;

            foreach (var cellView in cellViews)
            {
                cellView.Set(avatarState);
            }
        }

        private void SubScribeFilterType(StatType statType)
        {
            scrollRect.normalizedPosition = new Vector2(0.5f, 1.0f);

            foreach (var cellView in cellViews)
            {
                if (cellView.StatType == statType)
                {
                    cellView.Show();
                }
                else
                {
                    cellView.Hide();
                }
            }
        }

        private void SubscribeOnToggledOn(IToggleable toggleable)
        {
            if (toggleable.Name.Equals(hpTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(StatType.HP);
            }
            else if (toggleable.Name.Equals(atkTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(StatType.ATK);
            }
            else if (toggleable.Name.Equals(criTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(StatType.CRI);
            }
            else if (toggleable.Name.Equals(hitTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(StatType.HIT);
            }
            else if (toggleable.Name.Equals(defTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(StatType.DEF);
            }
        }

        private void SubscribeOnClickCellView(RecipeCellView cellView)
        {
            var combination = Widget.Find<Combination>();
            combination.selectedRecipe = cellView as ConsumableRecipeCellView;
            combination.State.SetValueAndForceNotify(Combination.StateType.CombinationConfirm);
        }
    }
}
