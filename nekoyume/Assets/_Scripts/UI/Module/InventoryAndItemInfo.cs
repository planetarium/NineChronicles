using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class InventoryAndItemInfo : MonoBehaviour
    {
        public Inventory inventory;
        public ItemInfo itemInfo;
        
        private Model.InventoryAndItemInfo _data;

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
        
        public void SetData(Model.InventoryAndItemInfo data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;       
            }
            
            _data = data;
            inventory.SetData(_data.inventory.Value);
            itemInfo.SetData(_data.itemInfo.Value);
        }
        
        public void Clear()
        {
            inventory.Clear();
            itemInfo.Clear();
            _data = null;
        }
    }
}
