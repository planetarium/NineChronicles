using Nekoyume.UI.Scroller;
using Nekoyume.Model.Item;
using UniRx;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentRecipe : MonoBehaviour
    {
        public EquipmentRecipeCellView cellViewPrefab;
        public EquipmentRecipeCellView[] cellViews;
        public TabButton weaponTabButton;
        public TabButton armorTabButton;
        public TabButton beltTabButton;
        public TabButton necklaceTabButton;
        public TabButton ringTabButton;
        public Transform cellViewParent;

        public ScrollRect scrollRect;

        public readonly ReactiveProperty<ItemSubType> FilterType =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        public EquipmentRecipeCellView selectedRecipe;

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private void Awake()
        {
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(weaponTabButton);
            _toggleGroup.RegisterToggleable(armorTabButton);
            _toggleGroup.RegisterToggleable(beltTabButton);
            _toggleGroup.RegisterToggleable(necklaceTabButton);
            _toggleGroup.RegisterToggleable(ringTabButton);

            LoadRecipeList();
            FilterType.Subscribe(SubScribeFilterType).AddTo(gameObject);
        }

        private void LoadRecipeList()
        {
            var recipeSheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;

            var totalCount = recipeSheet.Count();
            cellViews = new EquipmentRecipeCellView[totalCount];

            var idx = 0;
            foreach (var recipeRow in recipeSheet)
            {
                cellViews[idx] = Instantiate(cellViewPrefab, cellViewParent);
                cellViews[idx].Set(recipeRow, true);
                cellViews[idx].OnClick.Subscribe(SubscribeOnClickCellView).AddTo(gameObject);
                ++idx;
            }
        }

        private void SubScribeFilterType(ItemSubType itemSubType)
        {
            scrollRect.normalizedPosition = new Vector2(0.5f, 1.0f);

            foreach (var data in cellViews)
            {
                data.Hide();
            }

            var filteredRecipe = cellViews.Where(cellView => cellView.itemSubType == itemSubType);

            // FIXME : 테이블이 완성된 후 대응시켜야 함.
            foreach (var filtered in filteredRecipe)
            {
                filtered.Show();
            }

            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                    _toggleGroup.SetToggledOn(weaponTabButton);

                    break;
                case ItemSubType.Armor:
                    _toggleGroup.SetToggledOn(armorTabButton);

                    break;
                case ItemSubType.Belt:
                    _toggleGroup.SetToggledOn(beltTabButton);

                    break;
                case ItemSubType.Necklace:
                    _toggleGroup.SetToggledOn(necklaceTabButton);

                    break;
                case ItemSubType.Ring:
                    _toggleGroup.SetToggledOn(ringTabButton);

                    break;
                default:
                    break;
            }
        }

        private void SubscribeOnToggledOn(IToggleable toggleable)
        {
            if (toggleable.Name.Equals(weaponTabButton.Name))
            {
                FilterType.SetValueAndForceNotify(ItemSubType.Weapon);
            }
            else if (toggleable.Name.Equals(armorTabButton.Name))
            {
                FilterType.SetValueAndForceNotify(ItemSubType.Armor);
            }
            else if (toggleable.Name.Equals(beltTabButton.Name))
            {
                FilterType.SetValueAndForceNotify(ItemSubType.Belt);
            }
            else if (toggleable.Name.Equals(necklaceTabButton.Name))
            {
                FilterType.SetValueAndForceNotify(ItemSubType.Necklace);
            }
            else if (toggleable.Name.Equals(ringTabButton.Name))
            {
                FilterType.SetValueAndForceNotify(ItemSubType.Ring);
            }
        }

        private void SubscribeOnClickCellView(EquipmentRecipeCellView cellView)
        {
            selectedRecipe = cellView;
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombinationConfirm);
        }
    }
}
