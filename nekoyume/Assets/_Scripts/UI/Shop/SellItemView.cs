using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    using UniRx;

    public class SellItemView : BaseShopItemView
    {
        [SerializeField]
        private TMP_Dropdown itemSubTypeFilter = null;

        [SerializeField]
        private TMP_Dropdown sortFilter = null;

        private ShopItemViewModel _selectedModel;

        // search condition
        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new ReactiveProperty<ItemSubTypeFilter>(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
            new ReactiveProperty<ShopSortFilter>(ShopSortFilter.Class);

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
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
                        return ShopSortFilter.Class;
                    }
                })
                .Subscribe(filter => _selectedSortFilter.Value = filter).AddTo(gameObject);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _selectedSortFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItemViewModel item)
        {
            if (_selectedModel == null)
            {
                _selectedModel = item;
                _selectedModel.Selected.SetValueAndForceNotify(true);
                ClickItemAction?.Invoke(_selectedModel, _selectedModel.View); // Show tooltip popup
            }
            else
            {
                if (_selectedModel.Equals(item))
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = null;
                }
                else
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = item;
                    _selectedModel.Selected.SetValueAndForceNotify(true);
                    ClickItemAction?.Invoke(_selectedModel,
                        _selectedModel.View); // Show tooltip popup
                }
            }
        }

        protected override void Reset()
        {
            itemSubTypeFilter.SetValueWithoutNotify(0);
            sortFilter.SetValueWithoutNotify(0);
            _selectedSubTypeFilter.Value = ItemSubTypeFilter.All;
            _selectedSortFilter.Value = ShopSortFilter.Class;
            _selectedModel = null;
        }

        protected override IEnumerable<ShopItemViewModel> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> items)
        {
            var models = items[_selectedSubTypeFilter.Value];
            return _selectedSortFilter.Value switch
            {
                ShopSortFilter.CP => models.OrderByDescending(x => x.OrderDigest.CombatPoint)
                    .ToList(),
                ShopSortFilter.Price => models.OrderByDescending(x => x.OrderDigest.Price).ToList(),
                ShopSortFilter.Class => models.OrderByDescending(x => x.Grade)
                    .ThenByDescending(x => x.ItemBase.ItemType).ToList(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
