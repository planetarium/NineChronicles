using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class IAPRewardView : MonoBehaviour
    {
        [field: SerializeField]
        public Image RewardGrade { get; private set; }

        [field: SerializeField]
        public Image RewardImage { get; private set; }

        [field: SerializeField]
        public TextMeshProUGUI RewardCount { get; private set; }

        private ItemBase itemBaseForToolTip = null;

        private FungibleAssetValueSchema fungibleAssetValue = null;

        public void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (itemBaseForToolTip != null)
                {
                    AudioController.PlayClick();
                    var tooltip = ItemTooltip.Find(itemBaseForToolTip.ItemType);
                    tooltip.Show(itemBaseForToolTip, string.Empty, false, null);
                }

                if (fungibleAssetValue != null)
                {
                    AudioController.PlayClick();
                    Widget.Find<FungibleAssetTooltip>().Show(fungibleAssetValue.Ticker, ((BigInteger)fungibleAssetValue.Amount).ToCurrencyNotation(), null);
                }
            });
        }

        public void SetFavItem(FungibleAssetValueSchema fav)
        {
            fungibleAssetValue = fav;
            itemBaseForToolTip = null;
        }

        public void SetItemBase(ItemBase itemBase)
        {
            itemBaseForToolTip = itemBase;
            fungibleAssetValue = null;
        }
    }
}
