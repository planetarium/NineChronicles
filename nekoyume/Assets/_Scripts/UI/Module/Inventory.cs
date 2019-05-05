using System;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        public InventoryScrollerController scrollerController;
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion
        
        public void SetData(Model.Inventory data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            scrollerController.SetData(data.items);
        }

        public void Clear()
        {
            scrollerController.Clear();
        }
    }
}
