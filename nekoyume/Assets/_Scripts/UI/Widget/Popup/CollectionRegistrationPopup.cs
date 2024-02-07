using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI
{
    using UniRx;
    public class CollectionRegistrationPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private CollectionStat collectionStat;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton registrationButton;
        [SerializeField] private CollectionInventory collectionInventory;
        [SerializeField] private EquipmentTooltip equipmentTooltip;

        private readonly Dictionary<CollectionMaterial, ICollectionMaterial> _registeredItems = new();
        private CollectionMaterial _focusedRequiredItem;
        private Action<List<ICollectionMaterial>> _registerMaterials;

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
                .Select(_ => collectionInventory.SelectedItem)
                .Subscribe(RegisterItem)
                .AddTo(gameObject);

            collectionInventory.SetInventory(OnClickInventoryItem);
        }

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
                        SkillContains = equipment.Skills.Any()
                    };
                    break;
                case Consumable consumable:
                    collectionMaterialItem = new FungibleCollectionMaterial
                    {
                        ItemId = consumable.Id,
                        ItemCount = _focusedRequiredItem.Row.Count,
                    };
                    break;
                case Material material:
                    collectionMaterialItem = new FungibleCollectionMaterial
                    {
                        ItemId = material.Id,
                        ItemCount = _focusedRequiredItem.Row.Count,
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
                OnClickItem(notRegisteredItem);
            }
            else
            {
                var registeredItems = _registeredItems.Values.ToList();
                _registerMaterials?.Invoke(registeredItems);
                CloseWidget.Invoke();
            }
        }

        private void OnClickInventoryItem(InventoryItem item)
        {
            ShowItemTooltip(item);
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

        private void OnClickItem(CollectionMaterial collectionMaterial)
        {
            _focusedRequiredItem?.Focused.SetValueAndForceNotify(false);
            _focusedRequiredItem = collectionMaterial;
            _focusedRequiredItem.Focused.SetValueAndForceNotify(true);

            collectionInventory.SetRequiredItem(_focusedRequiredItem);

            var canRegister = _registeredItems.Any(registeredItem => registeredItem.Value == null);
            registrationButton.Text = canRegister
                ? "Register"
                : "Activate";
        }

        public void Show(
            CollectionModel model,
            Action<List<ICollectionMaterial>> register,
            bool ignoreShowAnimation = false)
        {
            collectionStat.Set(model);
            _registerMaterials = register;

            var materialCount = model.Row.Materials.Count;

            _registeredItems.Clear();
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                var material = model.Materials[i];
                var data = new CollectionMaterial(material.Row, material.Grade, material.ItemType);
                collectionItemViews[i].Set(data ,OnClickItem);
                _registeredItems.Add(data, null);
            }

            OnClickItem(_registeredItems.Keys.First());

            base.Show(ignoreShowAnimation);
        }
    }
}
