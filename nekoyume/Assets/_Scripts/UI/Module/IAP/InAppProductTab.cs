using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Toggle))]
    public class InAppProductTab : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI disabledText;

        [SerializeField]
        private TextMeshProUGUI enabledText;

        [field:SerializeField]
        public Toggle Toggle { get; private set; }

        public string ProductId { get; private set; }
        public int DisplayOrder { get; private set; }

        public void Set(Product product, int displayOrder)
        {
            disabledText.text = enabledText.text = product.metadata.localizedTitle;
            ProductId = product.definition.id;
            DisplayOrder = displayOrder;
        }
    }
}
