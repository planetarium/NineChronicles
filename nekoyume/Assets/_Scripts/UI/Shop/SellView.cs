using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume
{
    using UniRx;

    public class SellView : ShopView
    {
        [SerializeField]
        private TMP_Dropdown itemSubTypeFilter;

        [SerializeField]
        private TMP_Dropdown sortFilter;

        private ShopItem _selectedItem;

        // search condition
        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new(ShopSortFilter.CP);

        public void ClearSelectedItem()
        {
            _selectedItem?.Selected.SetValueAndForceNotify(false);
            _selectedItem = null;
        }

        protected override void OnAwake() { }

        protected override void InitInteractiveUI()
        {
            itemSubTypeFilter.AddOptions(ItemSubTypeFilterExtension.Filters
                .Select(type => type.TypeToString(true)).ToList());
            itemSubTypeFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ItemSubTypeFilter)index;
                    }
                    catch
                    {
                        return ItemSubTypeFilter.All;
                    }
                })
                .Subscribe(filter => _selectedSubTypeFilter.Value = filter).AddTo(gameObject);

            sortFilter.AddOptions(ShopSortFilterExtension.ShopSortFilters
                .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}")).ToList());
            sortFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ShopSortFilter)index;
                    }
                    catch
                    {
                        return ShopSortFilter.CP;
                    }
                })
                .Subscribe(filter => _selectedSortFilter.Value = filter).AddTo(gameObject);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(_ =>
            {
                _page.SetValueAndForceNotify(0);
                UpdateView();
            }).AddTo(gameObject);
            _selectedSortFilter.Subscribe(_ =>
            {
                _page.SetValueAndForceNotify(0);
                UpdateView();
            }).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItem item)
        {
            if (_selectedItem == null)
            {
                _selectedItem = item;
                _selectedItem.Selected.SetValueAndForceNotify(true);
                ClickItemAction?.Invoke(_selectedItem); // Show tooltip popup
            }
            else
            {
                if (_selectedItem.Equals(item))
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = null;
                }
                else
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = item;
                    _selectedItem.Selected.SetValueAndForceNotify(true);
                    ClickItemAction?.Invoke(_selectedItem); // Show tooltip popup
                }
            }
        }

        protected override void Reset()
        {
            itemSubTypeFilter.SetValueWithoutNotify(0);
            sortFilter.SetValueWithoutNotify(0);
            _page.SetValueAndForceNotify(0);
            _selectedSubTypeFilter.Value = ItemSubTypeFilter.All;
            _selectedSortFilter.Value = ShopSortFilter.CP;
            _selectedItem = null;
        }

        protected override IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items)
        {
            var models = items[_selectedSubTypeFilter.Value].Distinct();
            var fungibleAssetProducts = models.Where(x => x.Product is null);
            var itemProducts = models.Where(x => x.Product is not null).ToList();

            var result = new List<ShopItem>();
            switch (_selectedSortFilter.Value)
            {
                case ShopSortFilter.CP:
                    result = itemProducts
                        .OrderByDescending(x => x.Product.CombatPoint)
                        .ToList();
                    break;
                case ShopSortFilter.Price:
                    result = itemProducts
                        .OrderByDescending(x => x.Product.Price)
                        .ToList();
                    break;
                case ShopSortFilter.Class:
                    result = itemProducts
                        .OrderByDescending(x => x.Grade)
                        .ThenByDescending(x => x.ItemBase.ItemType)
                        .ToList();
                    break;
                case ShopSortFilter.Crystal:
                    result = itemProducts
                        .OrderByDescending(x => x.ItemBase.ItemType == ItemType.Equipment
                            ? CrystalCalculator.CalculateCrystal(
                                    new[] { (Equipment)x.ItemBase },
                                    false,
                                    TableSheets.Instance.CrystalEquipmentGrindingSheet,
                                    TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                                    States.Instance.StakingLevel).DivRem((BigInteger)x.Product.Price)
                                .Quotient
                                .MajorUnit
                            : 0).ToList();
                    break;
            }

            result.AddRange(fungibleAssetProducts);
            return result;
        }
    }
}
