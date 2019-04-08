using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class InventoryRenew : Widget
    {
        public InventoryScrollerController scrollerController;

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(scrollerController, null))
            {
                throw new SerializeFieldNullException();
            }
        }
        
        #endregion
        
        public void SetData(Model.Inventory data)
        {
            scrollerController.SetData(data.Items);
        }
    }
}
