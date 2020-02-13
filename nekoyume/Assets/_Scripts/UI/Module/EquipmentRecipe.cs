using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Nekoyume.Model.Item;
using System.Linq;

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

        public readonly ReactiveProperty<ItemSubType> FilterType =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        // Start is called before the first frame update
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
            var sheet = Game.Game.instance.TableSheets.EquipmentItemSheet;

            var totalCount = sheet.Count();
            cellViews = new EquipmentRecipeCellView[totalCount];

            int idx = 0;
            foreach (var row in sheet)
            {
                cellViews[idx] = Instantiate(cellViewPrefab, cellViewParent);
                cellViews[idx].Set(new Equipment(row, new System.Guid()));
                ++idx;
            }
        }

        private void SubScribeFilterType(ItemSubType itemSubType)
        {
            foreach (var data in cellViews)
            {
                data.Hide();
            }

            var filteredRecipe = cellViews.Where(cellview => cellview.model.Data.ItemSubType == itemSubType);

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
    }
}
