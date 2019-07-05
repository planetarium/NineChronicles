using System;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        public InventoryScrollerController scrollerController;
        
        private ItemInformationTooltip _tooltip;
        private Model.ItemInformationTooltip _tooltipModel;
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();
            _tooltipModel = new Model.ItemInformationTooltip();
        }

        private void OnEnable()
        {
            _tooltip = Widget.Find<ItemInformationTooltip>();
        }

        private void OnDisable()
        {
            if (!ReferenceEquals(_tooltip, null))
            {
                _tooltip.Close();   
            }
        }

        private void OnDestroy()
        {
            _tooltipModel.Dispose();
            _tooltipModel = null;
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

        public void ShowTooltip(InventoryItem value)
        {
            if (value is null)
            {
                return;
            }
            
            _tooltipModel.itemInformation.item.Value = value;
            _tooltipModel.target.Value = GetComponent<RectTransform>();
            if (!ReferenceEquals(_tooltip, null))
            {
                _tooltip.Show(_tooltipModel);   
            }
        }
    }
}
