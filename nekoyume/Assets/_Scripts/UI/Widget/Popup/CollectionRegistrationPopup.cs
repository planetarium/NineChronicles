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

        private List<ICollectionMaterial> _materials;
        private Action<List<ICollectionMaterial>> _register;

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
                AudioController.PlayClick();
                _register?.Invoke(_materials);
                CloseWidget.Invoke();
            }).AddTo(gameObject);
        }

        public void Show(
            Collection.Model model,
            Action<List<ICollectionMaterial>> register,
            bool ignoreShowAnimation = false)
        {
            collectionStat.Set(model);

            _materials = new List<ICollectionMaterial>();
            _register = register;

            var materialCount = model.Row.Materials.Count;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                var material = model.Row.Materials[i];
                var itemRow = itemSheet[material.ItemId];
                var items = inventory.Items.Where(item => item.item.Id == material.ItemId).ToArray();
                var equipments = items.Select(item => item.item).OfType<Equipment>().ToArray();

                collectionItemViews[i].Set(
                    new CollectionMaterial(
                        itemRow, items.Any(),
                        material.Level, equipments.Any() ||
                                        equipments.Any(item => item.level == material.Level),
                        material.Count, items.Any() || items.Length > material.Count),
                    model => { });
            }

            base.Show(ignoreShowAnimation);
        }
    }
}
