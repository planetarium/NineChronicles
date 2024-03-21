using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class CollectionRegistrationPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private CollectionStat collectionStat;
        [SerializeField] private CanvasGroup collectionStatCanvasGroup;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton registrationButton;
        [SerializeField] private CollectionInventory collectionInventory;
        [SerializeField] private EquipmentTooltip equipmentTooltip;

        [Header("Register Animation")]
        [SerializeField]
        private float registerItemDelay = 0.05f;

        [SerializeField]
        private float registerAnimationDelay = 0.5f;

        private readonly Dictionary<CollectionMaterial, ICollectionMaterial> _registeredItems = new();
        private CollectionMaterial _focusedRequiredItem;
        private Action<List<ICollectionMaterial>> _registerMaterials;

        private bool canRegister;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close();
            };

            registrationButton.OnSubmitSubject
                .Subscribe(_ => OnClickRegisterButton())
                .AddTo(gameObject);

            collectionInventory.SetInventory(OnClickInventoryItem);
        }

        private void OnClickRegisterButton()
        {
            if (canRegister)
            {
                if (collectionInventory.SelectedItem.Equipped.Value)
                {
                    var confirm = Find<IconAndButtonSystem>();
                    confirm.ShowWithTwoButton(
                        "UI_WARNING", "UI_COLLECTION_REGISTRATION_CAUTION_PHRASE");
                    confirm.ConfirmCallback = () => RegisterItem(collectionInventory.SelectedItem);
                    confirm.CancelCallback = () => confirm.Close();
                }
                else
                {
                    RegisterItem(collectionInventory.SelectedItem);
                }
            }
            else
            {
                RegisterMaterials();
            }
        }

        private void OnClickInventoryItem(InventoryItem item)
        {
            if (!canRegister)
            {
                return;
            }

            ShowItemTooltip(item);
        }

        private void RegisterMaterials()
        {
            var registeredItems = _registeredItems.Values.ToList();
            _registerMaterials?.Invoke(registeredItems);
            RegisterItemAsync().Forget();
        }

        private async UniTask RegisterItemAsync()
        {
            var requiredItems = _registeredItems.Keys.ToArray();
            for (var i = 0; i < requiredItems.Length; i++)
            {
                requiredItems[i].Focused.SetValueAndForceNotify(false);
                requiredItems[i].Registered.SetValueAndForceNotify(true);
                await UniTask.Delay(TimeSpan.FromSeconds(registerItemDelay));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(registerAnimationDelay));
            CloseWidget.Invoke();
        }

        #region NonFungibleCollectionMaterial (Equipment, Costume)

        private void RegisterItem(InventoryItem item)
        {
            ICollectionMaterial collectionMaterialItem;
            switch (item.ItemBase)
            {
                case Equipment equipment:
                    collectionMaterialItem = new NonFungibleCollectionMaterial
                    {
                        ItemId = equipment.Id,
                        ItemCount = 1,
                        NonFungibleId = equipment.NonFungibleId,
                        Level = equipment.level,
                        SkillContains = equipment.HasSkill()
                    };
                    break;
                case Costume costume:
                    collectionMaterialItem = new NonFungibleCollectionMaterial
                    {
                        ItemId = item.ItemBase.Id,
                        ItemCount = 1,
                        NonFungibleId = costume.NonFungibleId,
                        SkillContains = false,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _registeredItems[_focusedRequiredItem] = collectionMaterialItem;

            // Focus next required item or Activate
            var notRegisteredItem = _registeredItems
                .FirstOrDefault(registeredItem => registeredItem.Value == null).Key;
            if (notRegisteredItem != null)
            {
                FocusItem(notRegisteredItem);
            }
            else
            {
                RegisterMaterials();
            }
        }

        private void ShowItemTooltip(InventoryItem item)
        {
            if (item.ItemBase is null)
            {
                return;
            }

            equipmentTooltip.Show(item, string.Empty, false, null);
            equipmentTooltip.OnEnterButtonArea(true);
        }

        private void FocusItem(CollectionMaterial collectionMaterial)
        {
            if (collectionMaterial == null || !canRegister)
            {
                return;
            }

            _focusedRequiredItem?.Focused.SetValueAndForceNotify(false);
            _focusedRequiredItem = collectionMaterial;
            collectionInventory.SetRequiredItem(_focusedRequiredItem);

            _focusedRequiredItem.Focused.SetValueAndForceNotify(true);

            var count = _registeredItems.Count(registeredItem => registeredItem.Value == null);
            registrationButton.Text = count == 1
                ? L10nManager.Localize("UI_ACTIVATE")
                : L10nManager.Localize("UI_REGISTER");
        }

        #endregion

        // For NonFungibleCollectionMaterial (Equipment, Costume)
        public void ShowForNonFungibleMaterial(
            CollectionModel model,
            Action<List<ICollectionMaterial>> register,
            bool ignoreShowAnimation = false)
        {
            collectionStat.Set(model);
            _registerMaterials = register;
            SetCanRegister(true);

            _registeredItems.Clear();
            foreach (var material in model.Materials)
            {
                var data = new CollectionMaterial(material.Row, material.Grade, material.ItemType);

                _registeredItems.Add(data, null);
            }

            var requiredItems = _registeredItems.Keys.ToArray();
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < requiredItems.Length);
                if (i >= requiredItems.Length)
                {
                    continue;
                }

                collectionItemViews[i].Set(requiredItems[i], FocusItem);
            }

            base.Show(ignoreShowAnimation);

            FocusItem(requiredItems.First());
        }

        // For FungibleCollectionMaterial (Consumable, Material)
        // fungible 하기 때문에 Inventory와 연동 없이 바로 등록
        public void ShowForFungibleMaterial(
            CollectionModel model,
            Action<List<ICollectionMaterial>> register,
            bool ignoreShowAnimation = false)
        {
            collectionStat.Set(model);
            _registerMaterials = register;
            SetCanRegister(false);

            _registeredItems.Clear();
            foreach (var material in model.Materials)
            {
                var required = new CollectionMaterial(material.Row, material.Grade, material.ItemType);
                var registered = new FungibleCollectionMaterial
                {
                    ItemId = material.Row.ItemId,
                    ItemCount = material.Row.Count,
                };

                _registeredItems.Add(required, registered);
            }

            var requiredItems = _registeredItems.Keys.ToArray();
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < requiredItems.Length);
                if (i >= requiredItems.Length)
                {
                    continue;
                }

                collectionItemViews[i].Set(requiredItems[i], null);
                requiredItems[i].Focused.SetValueAndForceNotify(true);
            }

            collectionInventory.SetRequiredItems(requiredItems);
            equipmentTooltip.Close();
            registrationButton.Text = L10nManager.Localize("UI_ACTIVATE");

            base.Show(ignoreShowAnimation);
        }

        private void SetCanRegister(bool value)
        {
            canRegister = value;
            collectionStatCanvasGroup.interactable = value;
            collectionStatCanvasGroup.blocksRaycasts = value;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            _registerMaterials = null;
            _registeredItems.Clear();
            collectionInventory.SetRequiredItem(null);
        }
    }
}
