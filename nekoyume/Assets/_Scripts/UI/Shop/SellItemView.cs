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
        private ItemSubTypeFilter _activeSubTypeFilter = ItemSubTypeFilter.All;
        private ShopSortFilter _activeSortFilter = ShopSortFilter.Class;

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
        }

        protected override void OnAwake()
        {
            itemSubTypeFilter.AddOptions(ItemSubTypeFilterExtension.Filters
                .Select(type => type.TypeToString(true))
                .ToList());
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
                .Subscribe(filter =>
                {
                    _activeSubTypeFilter = filter;
                    UpdateView();
                })
                .AddTo(gameObject);

            sortFilter.AddOptions(ShopSortFilterExtension.ShopSortFilters
                .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}"))
                .ToList());
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
                .Subscribe(filter =>
                {
                    _activeSortFilter = filter;
                    UpdateView();
                })
                .AddTo(gameObject);
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

        protected override IEnumerable<ShopItemViewModel> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItemViewModel>> items)
        {
            var models = items[_activeSubTypeFilter];
            return _activeSortFilter switch
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
