using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class BigInventoryItemView : InventoryItemView
    {
        [SerializeField]
        private GameObject selectCheck;

        [SerializeField]
        private GameObject selectObject;

        public void SetSelectType(bool isBaseItem)
        {
            selectObject.SetActive(isBaseItem);
            selectCheck.SetActive(!isBaseItem);
        }
    }
}
