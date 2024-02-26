using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
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

        private int? _initializedAvatarIndex = null;

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
                itemTypeToggle.toggle.onClickToggle.AddListener(AudioController.PlayClick);
                itemTypeToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ =>
                    {
                        _currentItemType = itemTypeToggle.type;

                        var toggle = statToggles.First().toggle;
                        toggle.isOn = !toggle.isOn;

                        UpdateStatToggleView();
                    })
                    .AddTo(gameObject);
            }

            foreach (var statToggle in statToggles)
            {
                statToggle.toggle.onClickToggle.AddListener(AudioController.PlayClick);
                statToggle.toggle.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ =>
                    {
                        _currentStatType = statToggle.stat;

                        UpdateScrollView();
                    })
                    .AddTo(gameObject);
            }

            scroll.OnClickActiveButton.Subscribe(OnClickActiveButton).AddTo(gameObject);
            scroll.OnClickMaterial.Subscribe(SelectMaterial).AddTo(gameObject);
            collectionMaterialInfo.OnClickCloseButton
                .Subscribe(_ => SelectMaterial(null)).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);

            var toggle = itemTypeToggles.First().toggle;
            toggle.isOn = !toggle.isOn;

            UpdateToggleView();
            UpdateStatToggleView();
            UpdateScrollView();
        }

        public void TryInitialize()
        {
            var index = Game.Game.instance.States.CurrentAvatarKey;
            if (_initializedAvatarIndex == index)
            {
                return;
            }

            _initializedAvatarIndex = index;

            _models.Clear();
            _models.GenerateModels();

            UpdateEffectView();
            UpdateToggleDictionary();

            ReactiveAvatarState.Inventory.Subscribe(_ => OnUpdateInventory()).AddTo(gameObject);
        }

        #region ScrollView

        private void UpdateScrollView()
        {
            var items = _models
                .Where(model =>
                    model.ItemType == _currentItemType &&
                    model.Row.StatModifiers.Any(stat => IsInToggle(stat, _currentStatType)))
                .OrderByDescending(model => model.CanActivate).ToList();

            scroll.UpdateData(items,true);
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
                collectionMaterialInfo.Show(_selectedMaterial.Row);
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
                    .Subscribe(eval =>
                    {
                        if (eval.Exception is not null)
                        {
                            OneLineSystem.Push(
                                MailType.System,
                                L10nManager.Localize("NOTIFICATION_COLLECTION_FAIL"),
                                NotificationCell.NotificationType.Alert);
                        }

                        LoadingHelper.ActivateCollection.Value = 0;
                    });

                LoadingHelper.ActivateCollection.Value = collectionId;
                AudioController.instance.PlaySfx(AudioController.SfxCode.Heal);
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

        #endregion

        public void OnActionRender()
        {
            _models.UpdateActive(); // on action render

            UpdateEffectView(); // on update model.Active
        }

        private void OnUpdateInventory()
        {
            _models.UpdateMaterials(); // on update inventory

            UpdateToggleDictionary(); // on update model.CanActivate
            if (gameObject.activeSelf)
            {
                UpdateToggleView(); // on update toggleDictionary
                UpdateStatToggleView(); // on update toggleDictionary

                UpdateScrollView(); // on update model.CanActivate
            }
        }

        // on update model.Active
        private void UpdateEffectView()
        {
            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            collectionEffect.Set(
                collectionState.Ids.Count,
                collectionSheet.Count,
                collectionState.GetEffects(collectionSheet));
        }

        // on update model.CanActivate
        private void UpdateToggleDictionary()
        {
            // materials.Select(material => material.Active)로 전체 토글값을 Dict로 캐싱
            foreach (var (itemType, dict) in _filter)
            {
                foreach (var toggleStat in TabStatTypes)
                {
                    dict[toggleStat] = _models.Any(model =>
                        model.ItemType == itemType &&
                        model.Row.StatModifiers.Any(stat => IsInToggle(stat, toggleStat)) &&
                        model.CanActivate);
                }
            }
        }

        #region ToggleView

        private void UpdateToggleView()
        {
            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.SetNotification(
                    _filter[itemTypeToggle.type].Values.Any(value => value));
            }
        }

        private void UpdateStatToggleView()
        {
            foreach (var statToggle in statToggles)
            {
                statToggle.SetNotification(_filter[_currentItemType][statToggle.stat]);
            }
        }

        private static bool IsInToggle(StatModifier stat, StatType toggleStatType)
        {
            StatType[] etcTypes =
            {
                StatType.CRI,
                StatType.DRV, StatType.DRR, StatType.CDMG,
                StatType.ArmorPenetration, StatType.Thorn,
            };

            switch (toggleStatType)
            {
                // All
                case StatType.NONE:
                    return true;

                // StatTypes in Tab
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.SPD:
                    return toggleStatType == stat.StatType;

                // Etc
                default:
                    return etcTypes.Contains(stat.StatType);
            }
        }

        #endregion
    }
}
