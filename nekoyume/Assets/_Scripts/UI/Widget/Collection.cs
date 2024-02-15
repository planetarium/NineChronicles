using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Collection;
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
            public Image hasNotificationImage;

            public void SetNotification(bool hasNotification)
            {
                hasNotificationImage.enabled = hasNotification;
            }
        }

        [Serializable]
        private struct StatToggle
        {
            public StatType stat;
            public Toggle toggle;
            public Image hasNotificationImage;

            public void SetNotification(bool hasNotification)
            {
                hasNotificationImage.enabled = hasNotification;
            }
        }

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private ItemTypeToggle[] itemTypeToggles;

        [SerializeField]
        private StatToggle[] statToggles;

        [SerializeField]
        private CollectionEffect collectionEffect;

        [SerializeField]
        private CollectionScroll scroll;

        [SerializeField]
        private CollectionMaterialInfo collectionMaterialInfo;

        private CollectionMaterial _selectedMaterial;

        private ItemType _currentItemType;
        private StatType _currentStatType;

        private bool _initialized;

        private readonly List<CollectionModel> _models = new List<CollectionModel>();

        private readonly Dictionary<ItemType, Dictionary<StatType, bool>> _filter = new();
        private static readonly StatType[] TabStatTypes =
        {
            StatType.NONE,
            StatType.HP, StatType.ATK, StatType.DEF, StatType.HIT, StatType.SPD,
            StatType.CRI
        };

        public bool HasNotification => _filter.Values.Any(dict => dict.Values.Any(value => value));

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

            foreach (var itemType in Enum.GetValues(typeof(ItemType)).OfType<ItemType>())
            {
                _filter[itemType] = new Dictionary<StatType, bool>();
                foreach (var statType in TabStatTypes)
                {
                    _filter[itemType][statType] = false;
                }
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ =>
                    {
                        SetFilter(itemTypeToggle.type);

                        statToggles.First().toggle.isOn = true;
                        foreach (var statToggle in statToggles)
                        {
                            statToggle.SetNotification(_filter[_currentItemType][statToggle.stat]);
                        }
                    })
                    .AddTo(gameObject);
            }

            foreach (var statToggle in statToggles)
            {
                statToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ => SetFilter(_currentItemType, statToggle.stat))
                    .AddTo(gameObject);
            }

            scroll.OnClickActiveButton.Subscribe(OnClickActiveButton).AddTo(gameObject);
            scroll.OnClickMaterial.Subscribe(SelectMaterial).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);


            SetFilter();
        }

        public void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            OnUpdateState();
            ReactiveAvatarState.Inventory.Subscribe(_ => OnUpdateInventory()).AddTo(gameObject);
        }

        private void SetFilter(
            ItemType itemType = ItemType.Equipment,
            StatType statType = StatType.NONE)
        {
            _currentItemType = itemType;
            _currentStatType = statType;

            scroll.UpdateData(_models.Sort(_currentItemType, _currentStatType), true);
            SelectMaterial(null);
        }

        private void SelectMaterial(CollectionMaterial viewModel)
        {
            if (_selectedMaterial == null)
            {
                _selectedMaterial = viewModel;
                _selectedMaterial?.Selected.SetValueAndForceNotify(true);
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
                _selectedMaterial?.Selected.SetValueAndForceNotify(true);
            }

            if (_selectedMaterial != null)
            {
                // collectionMaterialInfo.Show(_selectedMaterial);
            }
            else
            {
                collectionMaterialInfo.Close();
            }
        }

        private void OnClickActiveButton(CollectionModel model)
        {
            // check collection - is active
            var collectionId = model.Row.Id;
            var collectionState = Game.Game.instance.States.CollectionState;
            if (collectionState.Ids.Contains(collectionId))
            {
                Debug.LogError("collection already active");
                return;
            }

            void Action(List<ICollectionMaterial> materials)
            {
                ActionManager.Instance.ActivateCollection(collectionId, materials)
                    .Subscribe(_ => LoadingHelper.ActivateCollection.Value = false);

                LoadingHelper.ActivateCollection.Value = true;
            }

            switch (model.ItemType)
            {
                case ItemType.Equipment:
                case ItemType.Costume:
                    Find<CollectionRegistrationPopup>().ShowForNonFungibleMaterial(model, Action);
                    break;
                case ItemType.Consumable:
                case ItemType.Material:
                    Find<CollectionRegistrationPopup>().ShowForFungibleMaterial(model, Action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnActionRender()
        {
            OnUpdateState();

            scroll.UpdateData(_models.Sort(_currentItemType, _currentStatType), true);
        }

        private void OnUpdateState()
        {
            _models.SetModels();

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            collectionEffect.Set(
                collectionState.Ids.Count,
                collectionSheet.Count,
                collectionState.GetEffects(collectionSheet));

            OnUpdateInventory();
        }

        // 원래 inventory를 구독했어야 하는데 임시로 state를 구독
        private void OnUpdateInventory()
        {
            // update collection models - materials 값을 정의
            _models.UpdateMaterials();

            // materials.Select(material => material.Active)로 전체 토글값을 Dict로 캐싱
            foreach (var (itemType, dict) in _filter)
            {
                foreach (var statType in TabStatTypes)
                {
                    dict[statType] = _models.Any(model =>
                        model.ItemType == itemType &&
                        model.Row.StatModifiers.Any(stat => stat.CheckStat(statType)) &&
                        model.CanActivate);
                }
            }

            // update toggle view
            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.SetNotification(_filter[itemTypeToggle.type].Values.Any(value => value));
            }
        }
    }
}
