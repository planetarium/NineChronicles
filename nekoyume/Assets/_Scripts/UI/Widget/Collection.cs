using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class Collection : Widget
    {
        [SerializeField] private Button backButton;

        [SerializeField] private CollectionScroll scroll;

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

            scroll.OnClickActiveButton
                .Select(vm => vm.Row)
                .Subscribe(ActivateCollectionAction)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionCellViewModels = new List<CollectionCell.ViewModel>();

            var sb = new System.Text.StringBuilder();
            foreach (var row in collectionSheet.Values)
            {
                collectionCellViewModels.Add(new CollectionCell.ViewModel
                {
                    Row = row,
                    Active = collectionState.Ids.Contains(row.Id)
                });

                var active = collectionState.Ids.Contains(row.Id);
                sb.AppendLine($"Id: {row.Id} (active : {active})");
                foreach (var material in row.Materials)
                {
                    sb.AppendLine($"  ItemId: {material.ItemId}, Level: {material.Level}, Count: {material.Count}");
                }

                sb.AppendLine();
                foreach (var statModifier in row.StatModifiers)
                {
                    sb.AppendLine($"  StatType: {statModifier.StatType}, Value: {statModifier.Value}");
                }
            }

            Debug.Log(sb);

            scroll.UpdateData(collectionCellViewModels, true);
        }

        private void ActivateCollectionAction(CollectionSheet.Row row)
        {
            // check collection - is active
            var collectionState = Game.Game.instance.States.CollectionState;
            if (collectionState.Ids.Contains(row.Id))
            {
                Debug.LogError("collection already active");
                return;
            }

            // check itemIds invalid - are in inventory
            var itemIds = new List<Guid>();
            foreach (var materialId in row.Materials)
            {
                var itemUsable = Game.Game.instance.States.CurrentAvatarState.inventory.Equipments
                    .FirstOrDefault(i => i.Id == materialId.ItemId);
                if (itemUsable is null)
                {
                    Debug.LogError("item not found");
                    return;
                }

                if (itemUsable.Equipped)
                {
                    Debug.LogError("warning - item is equipped");
                }

                itemIds.Add(itemUsable.ItemId);
            }

            ActionManager.Instance.ActivateCollection(row.Id, itemIds).Subscribe();
        }
    }
}
