using Libplanet.Types.Assets;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class FungibleAssetValueView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        private FungibleAssetValue _fungibleAssetValue;

        public FungibleAssetValue FungibleAssetValue => _fungibleAssetValue;

        public void SetData(FungibleAssetValue value)
        {
            _fungibleAssetValue = value;
            iconImage.enabled = true;
            iconImage.overrideSprite = _fungibleAssetValue.GetIconSprite();
            iconImage.SetNativeSize();
        }
    }
}
