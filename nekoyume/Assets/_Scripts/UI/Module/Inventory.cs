using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }
        public InventoryScrollerController scrollerController;
        
        public ItemInformationTooltip Tooltip { get; private set; }
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();
            RectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Tooltip = Widget.Find<ItemInformationTooltip>();
        }

        private void OnDisable()
        {
            if (!ReferenceEquals(Tooltip, null))
            {
                Tooltip.Close();   
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion
        
        public void SetData(Model.Inventory model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }
            
            scrollerController.SetData(model.items);
        }

        public void Clear()
        {
            scrollerController.Clear();
        }
    }
}
