using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class EquipmentInventory : MonoBehaviour
    {
        private enum Grade
        {
            All,
            Normal,
            Rare,
            Epic,
            Unique,
            Legend,
        }

        private enum Elemental
        {
            All,
            Normal,
            Fire,
            Water,
            Land,
            Wind,
        }
        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ItemSubType Type;
        }

        [SerializeField] private List<CategoryToggle> categoryToggles = null;
        [SerializeField] private InventoryScroll scroll = null;
        [SerializeField] private TMP_Dropdown gradeFilter = null;
        [SerializeField] private TMP_Dropdown elementalFilter = null;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
        private readonly Subject<EquipmentInventory> _onResetItems = new Subject<EquipmentInventory>();
        private ItemSubType _itemSubType;
        private Grade _grade = Grade.All;
        private Elemental _elemental = Elemental.All;

        public Model.EquipmentInventory SharedModel { get; set; }

        protected void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    SharedModel.State.Value = categoryToggle.Type;
                    SortedData(_grade, _elemental);
                });
            }

            SharedModel = new Model.EquipmentInventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(cell => SharedModel.SubscribeItemOnClick(cell.View as BigInventoryItemView))
                .AddTo(gameObject);

            gradeFilter.AddOptions(new[]
                {
                    Grade.All,
                    Grade.Normal,
                    Grade.Rare,
                    Grade.Epic,
                    Grade.Unique,
                    Grade.Legend,
                }
                .Select(type => type.ToString())
                .ToList());

            gradeFilter.onValueChanged.AsObservable()
                .Select(index => (Grade) index)
                .Subscribe(filter =>
                {
                    _grade = filter;
                    SortedData(_grade, _elemental);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);

            elementalFilter.AddOptions(new[]
                {
                    Elemental.All,
                    Elemental.Normal,
                    Elemental.Fire,
                    Elemental.Water,
                    Elemental.Land,
                    Elemental.Wind,
                }
                .Select(type => type.ToString())
                .ToList());
            elementalFilter.onValueChanged.AsObservable()
                .Select(index =>(Elemental) index)
                .Subscribe(filter =>
                {
                    _elemental = filter;
                    SortedData(_grade, _elemental);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);

            gradeFilter.OnPointerClickAsObservable().Subscribe(_ => AudioController.PlayClick())
                .AddTo(gameObject);
            elementalFilter.OnPointerClickAsObservable().Subscribe(_ => AudioController.PlayClick())
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            gradeFilter.value = 0;
            elementalFilter.value = 0;
            ReactiveAvatarState.Inventory.Subscribe(inventoryState =>
            {
                SharedModel.ResetItems(inventoryState);
                _onResetItems.OnNext(this);
                SortedData(_grade, _elemental);
            }).AddTo(_disposablesAtOnEnable);
        }

        private void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
            if (Widget.TryFind<ItemInformationTooltip>(out var tooltip))
            {
                tooltip.Close();
            }
        }

        private void OnDestroy()
        {
            SharedModel.Dispose();
            SharedModel = null;
        }

        private void SortedData(Grade grade, Elemental elemental)
        {
            IEnumerable<InventoryItem> result = SharedModel.Equipments[_itemSubType];
            if (grade != Grade.All)
            {
                var value = (int) grade;
                result = result.Where(x => x.ItemBase.Value.Grade == value);
            }

            if (elemental != Elemental.All)
            {
                var value = (int) elemental - 1;
                result = result.Where(x => (int)x.ItemBase.Value.ElementalType == value);
            }
            scroll.UpdateData(result, true);
        }

        private void SubscribeState(ItemSubType type)
        {
            scroll.UpdateData(SharedModel.Equipments[type], type != _itemSubType);
            _itemSubType = type;

            if (Widget.TryFind<ItemInformationTooltip>(out var tooltip))
            {
                tooltip.Close();
            }
        }

        private void SubscribeSelectedItemView(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }

            scroll.ScrollTo(view.Model);
        }

        public void ClearItemState(Equipment equipment)
        {
            if (equipment is null)
            {
                return;
            }

            var item = SharedModel.Equipments[equipment.ItemSubType]
                .FirstOrDefault(x => ((Equipment)x.ItemBase.Value).ItemId.Equals(equipment.ItemId));

            if (item is null)
            {
                return;
            }

            item.Selected.SetValueAndForceNotify(false);
            item.EffectEnabled.SetValueAndForceNotify(false);
            item.Dimmed.SetValueAndForceNotify(false);
        }
    }
}
