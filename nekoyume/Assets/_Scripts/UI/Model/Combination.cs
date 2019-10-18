using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Manager;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        private static readonly ItemSubType[] DimmedTypes =
        {
            ItemSubType.Weapon,
            ItemSubType.RangedWeapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Helm,
            ItemSubType.Set,
            ItemSubType.Food,
            ItemSubType.Shoes
        };
        
        public readonly ReactiveProperty<ItemType> State =
            new ReactiveProperty<ItemType>(ItemType.Equipment);

        public readonly ReactiveProperty<bool> RecipeEnabled =
            new ReactiveProperty<bool>(false);

        public readonly ReactiveProperty<SimpleItemCountPopup> ItemCountPopup =
            new ReactiveProperty<SimpleItemCountPopup>();

        public readonly ReactiveProperty<CombinationMaterial> EquipmentMaterial =
            new ReactiveProperty<CombinationMaterial>();
        
        public readonly ReactiveCollection<CombinationMaterial> Materials =
            new ReactiveCollection<CombinationMaterial>();

        public readonly ReactiveProperty<int> ShowMaterialsCount = new ReactiveProperty<int>();
        public readonly ReactiveProperty<bool> ReadyToCombination = new ReactiveProperty<bool>();

        public readonly Subject<int> OnMaterialAdded = new Subject<int>();
        public readonly Subject<int> OnMaterialRemoved = new Subject<int>();
        
        public Combination()
        {
            ItemCountPopup.Value = new SimpleItemCountPopup();
            ItemCountPopup.Value.TitleText.Value =
                LocalizationManager.Localize("UI_COMBINATION_MATERIAL_COUNT_SELECTION");

            State.Subscribe(SubscribeState);
            ItemCountPopup.Value.OnClickSubmit.Subscribe(OnClickSubmitItemCountPopup);
            Materials.ObserveAdd().Subscribe(_ => OnMaterialAdd(_.Value));
            Materials.ObserveRemove().Subscribe(_ => OnMaterialRemove(_.Value));
        }

        public void Dispose()
        {
            State.Dispose();
            RecipeEnabled.Dispose();
            ItemCountPopup.DisposeAll();
            EquipmentMaterial.Dispose();
            Materials.DisposeAllAndClear();
            ShowMaterialsCount.Dispose();
            ReadyToCombination.Dispose();
        }

        private void SubscribeState(ItemType value)
        {
            switch (value)
            {
                case ItemType.Consumable:
                    ShowMaterialsCount.Value = 5;
                    break;
                case ItemType.Equipment:
                    ShowMaterialsCount.Value = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            RemoveEquipmentMaterial();
            
            while (Materials.Count > 0)
            {
                Materials.RemoveAt(0);
            }
        }

        private void OnClickSubmitItemCountPopup(SimpleItemCountPopup data)
        {
            if (ReferenceEquals(data, null)
                || ReferenceEquals(data.Item.Value, null))
            {
                ItemCountPopup.Value.Item.Value = null;
                return;
            }

            RegisterToStagedItems(data.Item.Value);
            ItemCountPopup.Value.Item.Value = null;
        }

        public void RemoveEquipmentMaterial()
        {
            if (EquipmentMaterial.Value == null)
            {
                return;
            }
            
            var temp = EquipmentMaterial.Value;
            EquipmentMaterial.Value = null;
            OnMaterialRemove(temp);
        }

        public void RemoveMaterialsAll()
        {
            RemoveEquipmentMaterial();
            
            while (Materials.Count > 0)
            {
                Materials.RemoveAt(0);
            }
        }

        public bool RegisterToStagedItems(CountableItem countEditableItem)
        {
            if (countEditableItem is null)
                return false;

            var sum = Materials
                .Where(item => countEditableItem.ItemBase.Value.Data.Id == item.ItemBase.Value.Data.Id)
                .Sum(item => item.Count.Value);

            if (sum >= countEditableItem.Count.Value)
                return false;

            if (State.Value == ItemType.Equipment
                && countEditableItem.ItemBase.Value.Data.ItemSubType == ItemSubType.EquipmentMaterial)
            {
                RemoveEquipmentMaterial();
                
                EquipmentMaterial.Value = new CombinationMaterial(
                    countEditableItem.ItemBase.Value,
                    1,
                    0,
                    countEditableItem.Count.Value);
                OnMaterialAdd(EquipmentMaterial.Value);
                
                return true;
            }

            foreach (var material in Materials)
            {
                if (material.ItemBase.Value.Data.Id != 0)
                {
                    continue;
                }

                if (countEditableItem.Count.Value == 0)
                {
                    Materials.Remove(material);
                }

                return true;
            }

            if (Materials.Count >= ShowMaterialsCount.Value)
                return false;

            Materials.Add(new CombinationMaterial(
                countEditableItem.ItemBase.Value,
                1,
                0,
                countEditableItem.Count.Value));
            
            return true;
        }

        private void OnMaterialAdd(CombinationMaterial value)
        {
            value.Count.Subscribe(count => UpdateReadyForCombination());
            value.OnMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                if (obj.Count.Value > 1)
                {
                    obj.Count.Value--;
                }
            });
            value.OnPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                var sum = Materials
                .Where(item => obj.ItemBase.Value.Data.Id == item.ItemBase.Value.Data.Id)
                .Sum(item => item.Count.Value);
                if (sum < obj.MaxCount.Value)
                {
                    obj.Count.Value++;
                }
            });
            value.OnDelete.Subscribe(obj =>
            {
                if (!(obj is CombinationMaterial material))
                {
                    return;
                }

                if (State.Value == ItemType.Equipment
                    && EquipmentMaterial.Value != null
                    && EquipmentMaterial.Value.ItemBase.Value.Data.Id == obj.ItemBase.Value.Data.Id)
                {
                    RemoveEquipmentMaterial();
                }
                else
                {
                    Materials.Remove(material);   
                }
                
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationRemoveMaterialItem);
            });

            OnMaterialAdded.OnNext(value.ItemBase.Value.Data.Id);
            UpdateReadyForCombination();
        }

        private void OnMaterialRemove(CombinationMaterial value)
        {
            value.Dispose();

            var materialId = value.ItemBase.Value.Data.Id;
            if (Materials.Any(item => item.ItemBase.Value.Data.Id == materialId))
            {
                OnMaterialAdded.OnNext(materialId);
            }
            else
            {
                OnMaterialRemoved.OnNext(materialId);
            }
            UpdateReadyForCombination();
        }

        private void UpdateReadyForCombination()
        {
            switch (State.Value)
            {
                case ItemType.Consumable:
                    ReadyToCombination.Value =
                        Materials.Count >= 2 && States.Instance.CurrentAvatarState.Value.actionPoint >=
                        Action.Combination.RequiredPoint;
                    break;
                case ItemType.Equipment:
                    ReadyToCombination.Value = 
                        EquipmentMaterial.Value != null
                        && Materials.Count >= 1
                        && States.Instance.CurrentAvatarState.Value.actionPoint >= Action.Combination.RequiredPoint;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
