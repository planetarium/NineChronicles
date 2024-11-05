using UnityEngine;

namespace Nekoyume.UI
{
    /// <summary>
    /// this class that exist to only create popup widgets.
    /// </summary>
    public class ChainInfoPopup : PopupWidget
    {
        [SerializeField]
        private ChainInfoItem chainInfoItem;
        
        protected override void Awake()
        {
            base.Awake();
            chainInfoItem.OnOpenDetailWebPage += OpenDetailWebPage;
        }
        
        private void OpenDetailWebPage()
        {
            Close();
        }
    }
}
