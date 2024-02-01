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

        private CollectionMaterial _focusedRequiredItem;
        private readonly List<CollectionMaterial> _requiredItems = new(); // required와 registered가 짝이 지어져야 함
        private readonly List<ICollectionMaterial> _registeredItems = new();
        private Action<List<ICollectionMaterial>> _activate;

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
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            registrationButton.OnSubmitSubject.Subscribe(_ =>
            {
                ICollectionMaterial material;
                var selectedItem = collectionInventory.SelectedItem;
                if (selectedItem.ItemBase is Equipment equipment)
                {
                    material = new NonFungibleCollectionMaterial
                    {
                        ItemId = equipment.Id,
                        ItemCount = 1,
                        NonFungibleId = equipment.NonFungibleId,
                        Level = equipment.level,
                        OptionCount = equipment.GetOptionCount(),
                        SkillContains = equipment.Skills.Any()
                    };
                }
                else
                {
                    material = new FungibleCollectionMaterial
                    {
                        ItemId = selectedItem.ItemBase.Id,
                        ItemCount = selectedItem.Count.Value,
                    };
                }

                _registeredItems.Add(material);

                var sb = new System.Text.StringBuilder();
                foreach (var item in _registeredItems)
                {
                    sb.Append(item.ItemId);
                    sb.AppendLine(", ");
                }

                Debug.LogError(sb);

                if (_registeredItems.Count < _requiredItems.Count)
                {
                    var first = _requiredItems.FirstOrDefault(collectionMaterial =>
                        !collectionMaterial.Selected.Value);
                    FocusRequiredMaterial(first);
                }
                else
                {
                    _activate?.Invoke(_registeredItems);
                    CloseWidget.Invoke();
                }
            }).AddTo(gameObject);

            collectionInventory.SetInventory(item =>
            {
                registrationButton.Interactable = item != null;
                registrationButton.Text = _registeredItems.Count < _requiredItems.Count
                    ? "Register"
                    : "Activate";
            }, (_, _) => { });
        }

        public void Show(
            Collection.Model model,
            Action<List<ICollectionMaterial>> register,
            bool ignoreShowAnimation = false)
        {
            collectionStat.Set(model);

            _registeredItems.Clear();
            _activate = register;

            _requiredItems.Clear();
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            foreach (var material in model.Row.Materials)
            {
                var itemRow = itemSheet[material.ItemId];
                var items = inventory.Items.Where(item => item.item.Id == material.ItemId).ToArray();
                var equipments = items.Select(item => item.item).OfType<Equipment>().ToArray();

                var collectionMaterial = new CollectionMaterial(
                    material, itemRow.Grade, items.Any(),
                    equipments.Any() || equipments.Any(item => item.level == material.Level),
                    items.Any() || items.Length > material.Count);
                _requiredItems.Add(collectionMaterial);
            }

            var materialCount = _requiredItems.Count;
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                collectionItemViews[i].Set(_requiredItems[i], FocusRequiredMaterial);
            }

            FocusRequiredMaterial(_requiredItems.First());

            base.Show(ignoreShowAnimation);
        }

        private void FocusRequiredMaterial(CollectionMaterial collectionMaterial)
        {
            _focusedRequiredItem?.Selected.SetValueAndForceNotify(false);
            _focusedRequiredItem = collectionMaterial;
            _focusedRequiredItem.Selected.SetValueAndForceNotify(true);
            collectionInventory.SetRequiredItem(_focusedRequiredItem);
        }
    }
}
