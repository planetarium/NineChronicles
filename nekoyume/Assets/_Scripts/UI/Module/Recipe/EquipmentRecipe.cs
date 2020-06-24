using System;
using System.Collections.Generic;
using Nekoyume.UI.Scroller;
using Nekoyume.Model.Item;
using UniRx;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Tween;

namespace Nekoyume.UI.Module
{
    public class EquipmentRecipe : MonoBehaviour
    {
        [SerializeField]
        private EquipmentRecipeCellView cellViewPrefab = null;

        [SerializeField]
        private EquipmentRecipeCellView[] cellViews = null;

        [SerializeField]
        private TabButton weaponTabButton = null;

        [SerializeField]
        private TabButton armorTabButton = null;

        [SerializeField]
        private TabButton beltTabButton = null;

        [SerializeField]
        private TabButton necklaceTabButton = null;

        [SerializeField]
        private TabButton ringTabButton = null;

        [SerializeField]
        private Transform cellViewParent = null;

        [SerializeField]
        private ScrollRect scrollRect = null;

        [SerializeField]
        private DOTweenGroupAlpha scrollAlphaTweener = null;

        [SerializeField]
        private AnchoredPositionYTweener scrollPositionTweener = null;

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly ReactiveProperty<ItemSubType> _filterType =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        private readonly List<IDisposable> _disposablesAtLoadRecipeList = new List<IDisposable>();

        private void Awake()
        {
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(weaponTabButton);
            _toggleGroup.RegisterToggleable(armorTabButton);
            _toggleGroup.RegisterToggleable(beltTabButton);
            _toggleGroup.RegisterToggleable(necklaceTabButton);
            _toggleGroup.RegisterToggleable(ringTabButton);

            LoadRecipes(false);
            _filterType.Subscribe(SubScribeFilterType).AddTo(gameObject);
        }

        private void OnEnable()
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

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

        public void HideCellViews()
        {
            scrollAlphaTweener.PlayReverse();
            scrollPositionTweener.PlayReverse();
            foreach (var view in cellViews)
            {
                view.SetInteractable(false);
            }
        }

        private void LoadRecipes(bool shouldUpdateRecipes = true)
        {
            _disposablesAtLoadRecipeList.DisposeAllAndClear();

            var recipeSheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            var totalCount = recipeSheet.Count;
            cellViews = new EquipmentRecipeCellView[totalCount];

            var idx = 0;
            foreach (var recipeRow in recipeSheet)
            {
                var cellView = Instantiate(cellViewPrefab, cellViewParent);
                cellView.Set(recipeRow);
                cellView.OnClick.Subscribe(SubscribeOnClickCellView)
                    .AddTo(_disposablesAtLoadRecipeList);
                cellViews[idx] = cellView;
                ++idx;
            }

            if (!shouldUpdateRecipes)
            {
                return;
            }

            UpdateRecipes();
        }

        public void UpdateRecipes()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            foreach (var cellView in cellViews)
            {
                cellView.Set(avatarState);
            }
        }

        private void SubScribeFilterType(ItemSubType itemSubType)
        {
            scrollRect.normalizedPosition = new Vector2(0.5f, 1.0f);

            foreach (var cellView in cellViews)
            {
                if (cellView.ItemSubType == itemSubType)
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
            if (toggleable.Name.Equals(weaponTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(ItemSubType.Weapon);
            }
            else if (toggleable.Name.Equals(armorTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(ItemSubType.Armor);
            }
            else if (toggleable.Name.Equals(beltTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(ItemSubType.Belt);
            }
            else if (toggleable.Name.Equals(necklaceTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(ItemSubType.Necklace);
            }
            else if (toggleable.Name.Equals(ringTabButton.Name))
            {
                _filterType.SetValueAndForceNotify(ItemSubType.Ring);
            }
        }

        private static void SubscribeOnClickCellView(RecipeCellView cellView)
        {
            var combination = Widget.Find<Combination>();
            combination.selectedRecipe = cellView as EquipmentRecipeCellView;
            combination.State.SetValueAndForceNotify(Combination.StateType.CombinationConfirm);
        }
    }
}
