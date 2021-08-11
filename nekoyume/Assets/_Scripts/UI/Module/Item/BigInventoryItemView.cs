using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class BigInventoryItemView : InventoryItemView
    {
        [SerializeField]
        private GameObject selectCheck;

        [SerializeField]
        private GameObject selectObject;

        public void Select(bool isBaseItem)
        {
            selectObject.SetActive(isBaseItem);
            selectCheck.SetActive(!isBaseItem);
            Model.Selected.SetValueAndForceNotify(true);
        }
    }
}
