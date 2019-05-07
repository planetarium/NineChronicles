using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class InventoryAndSelectedItemInfo : MonoBehaviour
    {
        public Inventory inventory;
        public ItemInfo selectedItemInfo;
        
        private Model.InventoryAndSelectedItemInfo _data;

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
        
        public void SetData(Model.InventoryAndSelectedItemInfo data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;       
            }
            
            _data = data;
            inventory.SetData(_data.inventory.Value);
            selectedItemInfo.SetData(_data.selectedItemInfo.Value);
        }
        
        public void Clear()
        {
            inventory.Clear();
            selectedItemInfo.Clear();
            _data = null;
        }
    }
}
