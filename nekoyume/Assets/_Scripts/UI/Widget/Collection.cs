using System;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;
    public class Collection : Widget
    {
        [Serializable]
        private struct ItemTypeToggle
        {
            public ItemType type;
            public Toggle toggle;
        }

        [Serializable]
        private struct StatToggle
        {
            public StatType stat;
            public Toggle toggle;
        }

        [SerializeField] private Button backButton;
        [SerializeField] private ItemTypeToggle[] itemTypeToggles;
        [SerializeField] private StatToggle[] statToggles;
        [SerializeField] private CollectionEffect collectionEffect;
        [SerializeField] private CollectionScroll scroll;
        // Todo : Item Info View

        private CollectionMaterial _selectedMaterial;
        private bool _initialized;

        private ItemType _currentItemType;
        private StatType _currentStatType;

        protected override void Awake()
        {
            base.Awake();

            backButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            scroll.OnClickActiveButton.Subscribe(ActivateCollectionAction).AddTo(gameObject);
            scroll.OnClickMaterial.Subscribe(OnClickMaterial).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            UpdateView();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);

            if (!_initialized)
            {
                _initialized = true;
                ReactiveAvatarState.Inventory.Subscribe(_ => UpdateView()).AddTo(gameObject);

                foreach (var itemTypeToggle in itemTypeToggles)
                {
                    itemTypeToggle.toggle.OnValueChangedAsObservable()
                        .Where(isOn => isOn)
                        .Subscribe(_ => Set(itemTypeToggle.type))
                        .AddTo(gameObject);
                }

                foreach (var statToggle in statToggles)
                {
                    statToggle.toggle.OnValueChangedAsObservable()
                        .Where(isOn => isOn)
                        .Subscribe(_ => Set(_currentItemType, statToggle.stat))
                        .AddTo(gameObject);
                }
            }
        }

        private void Set(
            ItemType itemType = ItemType.Equipment,
            StatType statType = StatType.NONE)
        {
            _currentItemType = itemType;
            _currentStatType = statType;

            UpdateView();
        }

        private void UpdateView()
        {
            var models = CollectionModel.GetModels()
                .Where(model =>
                    model.ItemType == _currentItemType &&
                    model.Row.StatModifiers.Any(stat => _currentStatType == StatType.NONE || stat.StatType == _currentStatType))
                .OrderByDescending(model => model.Active)
                .ToArray();
            scroll.UpdateData(models, true);
            collectionEffect.Set(models);
        }

        private void OnClickMaterial(CollectionMaterial viewModel)
        {
            if (_selectedMaterial == null)
            {
                _selectedMaterial = viewModel;
                _selectedMaterial.Selected.SetValueAndForceNotify(true);
            }
            else if (_selectedMaterial.Equals(viewModel))
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = null;
            }
            else
            {
                _selectedMaterial.Selected.SetValueAndForceNotify(false);
                _selectedMaterial = viewModel;
                _selectedMaterial.Selected.SetValueAndForceNotify(true);
            }

            // Todo : Show Item Info
        }

        private void ActivateCollectionAction(CollectionModel model)
        {
            // check collection - is active
            var collectionState = Game.Game.instance.States.CollectionState;
            if (collectionState.Ids.Contains(model.Row.Id))
            {
                Debug.LogError("collection already active");
                return;
            }

            // set materials
            Find<CollectionRegistrationPopup>().Show(model, materials =>
            {
                ActionManager.Instance.ActivateCollection(model.Row.Id, materials)
                    .Subscribe(_ => LoadingHelper.ActivateCollection.Value = false);
                LoadingHelper.ActivateCollection.Value = true;
            });
        }

        public void OnActionRender()
        {
            LoadingHelper.ActivateCollection.Value = false;
            UpdateView();
        }
    }
}
