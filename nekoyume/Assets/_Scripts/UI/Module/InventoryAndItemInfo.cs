using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class InventoryAndItemInfo : MonoBehaviour
    {
        public Inventory inventory;
        public ItemInfo itemInfo;
        
        private Model.Inventory _inventoryData;
        private Model.ItemInfo _itemInfoData;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion
        
        public void SetData(Model.Inventory inventoryData, Model.ItemInfo itemInfoData)
        {
            if (ReferenceEquals(inventory, null))
            {
                Clear();
                return;       
            }
            
            _inventoryData = inventoryData;
            _itemInfoData = itemInfoData;
            inventory.SetData(_inventoryData);
            itemInfo.SetData(_itemInfoData);
        }
        
        public void Clear()
        {
            inventory.Clear();
            itemInfo.Clear();
            _inventoryData = null;
            _itemInfoData = null;
        }
    }
}
