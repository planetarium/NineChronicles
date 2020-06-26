using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.UI.Scroller;
using Nekoyume.Model.Item;
using UniRx;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Tween;
using Nekoyume.Model.Quest;

namespace Nekoyume.UI.Module
{
    // FIXME: `ConsumableRecipe`과 거의 똑같은 구조입니다.
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

        private bool _initialized = false;
        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly ReactiveProperty<ItemSubType> _filterType =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        private readonly List<IDisposable> _disposablesAtLoadRecipeList = new List<IDisposable>();

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            UpdateRecipes();
        }

        private void OnDestroy()
        {
            _filterType.Dispose();
            _disposablesAtLoadRecipeList.DisposeAllAndClear();
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(weaponTabButton);
            _toggleGroup.RegisterToggleable(armorTabButton);
            _toggleGroup.RegisterToggleable(beltTabButton);
            _toggleGroup.RegisterToggleable(necklaceTabButton);
            _toggleGroup.RegisterToggleable(ringTabButton);

            LoadRecipes(false);
            _filterType.Subscribe(SubScribeFilterType).AddTo(gameObject);
        }

        public void ShowCellViews(int? recipeId = null)
        {
            if (recipeId.HasValue &&
                TryGetCellView(recipeId.Value, out var cellView) &&
                Game.Game.instance.TableSheets.EquipmentItemSheet.TryGetValue(
                    cellView.RowData.ResultEquipmentId,
                    out var row))
            {
                SetToggledOnType(row.ItemSubType);
                var content = scrollRect.content;
                var localPositionX = content.localPosition.x;
                content.localPosition = new Vector2(
                    localPositionX,
                    -cellView.transform.localPosition.y);
            }

            scrollAlphaTweener.Play();
            scrollPositionTweener.PlayTween();

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
                cellView.OnClick
                    .Subscribe(SubscribeOnClickCellView)
                    .AddTo(_disposablesAtLoadRecipeList);
                cellViews[idx++] = cellView;
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

            var quest = Game.Game.instance
                .States.CurrentAvatarState.questList?
                .OfType<CombinationEquipmentQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.RecipeId)
                .FirstOrDefault();

            weaponTabButton.HasNotification.Value = false;
            armorTabButton.HasNotification.Value = false;
            beltTabButton.HasNotification.Value = false;
            necklaceTabButton.HasNotification.Value = false;
            ringTabButton.HasNotification.Value = false;

            foreach (var cellView in cellViews)
            {
                var hasNotification = !(quest is null) && quest.RecipeId == cellView.RowData.Id;
                cellView.Set(avatarState, hasNotification);
                var btn = GetButton(cellView.ItemSubType);
                if (hasNotification)
                    btn.HasNotification.Value = hasNotification;
            }
        }

        public bool TryGetCellView(int recipeId, out EquipmentRecipeCellView cellView)
        {
            cellView = cellViews.FirstOrDefault(item => item.RowData.Id == recipeId);
            return !(cellView is null);
        }

        private TabButton GetButton(ItemSubType itemSubType)
        {
            TabButton btn = null;

            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                    btn = weaponTabButton;
                    break;
                case ItemSubType.Armor:
                    btn = armorTabButton;
                    break;
                case ItemSubType.Belt:
                    btn = beltTabButton;
                    break;
                case ItemSubType.Necklace:
                    btn = necklaceTabButton;
                    break;
                case ItemSubType.Ring:
                    btn = ringTabButton;
                    break;
                default:
                    break;
            }

            return btn;
        }

        private void SetToggledOnType(ItemSubType itemSubType)
        {
            IToggleable toggleable = GetButton(itemSubType);

            _toggleGroup.SetToggledOn(toggleable);
            SubscribeOnToggledOn(toggleable);
        }

        private void SubScribeFilterType(ItemSubType itemSubType)
        {
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

            scrollRect.normalizedPosition = new Vector2(0.5f, 1.0f);
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
            if (!combination.CanHandleInputEvent)
            {
                return;
            }

            combination.selectedRecipe = cellView;
            combination.State.SetValueAndForceNotify(Combination.StateType.CombinationConfirm);
        }
    }
}
