using Assets.SimpleLocalization;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        public Text titleText;
        public InventoryScrollerController scrollerController;
        
        public RectTransform RectTransform { get; private set; }
        
        public ItemInformationTooltip Tooltip { get; private set; }
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            
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
