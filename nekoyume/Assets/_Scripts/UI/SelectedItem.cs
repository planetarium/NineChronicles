using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SelectedItem : MonoBehaviour
    {
        public Text itemName;
        public Image priceIcon;
        public Text price;
        public Image icon;
        public Text info;
        public Text flavour;

        public ItemBase item;

        private bool _considerPrice = false;
        
        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(priceIcon, null) ||
                ReferenceEquals(price, null))
            {
                _considerPrice = false;
            }
            else
            {
                _considerPrice = true;
            }
        }

        #endregion

        public void SetItem(ItemBase itemBase)
        {
            item = itemBase;
            itemName.text = itemBase.Data.GetLocalizedName();
            info.text = itemBase.ToItemInfo();
            flavour.text = itemBase.Data.GetLocalizedDescription();

            if (_considerPrice)
            {
                priceIcon.enabled = true;
                price.text = "1";
            }
        }

        public void SetIcon(Sprite sprite)
        {
            icon.enabled = true;
            icon.overrideSprite = sprite;
            icon.SetNativeSize();
        }

        public void Clear()
        {
            item = null;
            itemName.text = LocalizationManager.Localize("UI_ITEM_INFORMATION");
            icon.enabled = false;
            info.text = LocalizationManager.Localize("UI_SELECT_AN_ITEM");
            flavour.text = "";

            if (_considerPrice)
            {
                priceIcon.enabled = false;
                price.text = "";
            }
        }
    }
}
