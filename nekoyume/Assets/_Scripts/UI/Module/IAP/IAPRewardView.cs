using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
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

        private InAppPurchaseServiceClient.FungibleAssetValueSchema fungibleAssetValue = null;

        public void Awake()
        {
            var button = GetComponent<Button>();
            if (button == null )
            {
                return;
            }

            button.onClick.AddListener(() =>
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

        public void SetFavItem(InAppPurchaseServiceClient.FungibleAssetValueSchema fav)
        {
            fungibleAssetValue = fav;
            itemBaseForToolTip = null;
            gameObject.SetActive(true);
            RewardImage.sprite = SpriteHelper.GetFavIcon(fungibleAssetValue.Ticker);
            RewardCount.text = ((BigInteger)fungibleAssetValue.Amount).ToCurrencyNotation();
            RewardGrade.sprite = SpriteHelper.GetItemBackground(Util.GetTickerGrade(fungibleAssetValue.Ticker));
        }

        public void SetItemBase(InAppPurchaseServiceClient.FungibleItemSchema itemBase)
        {
            gameObject.SetActive(true);
            RewardImage.sprite = SpriteHelper.GetItemIcon(itemBase.SheetItemId);
            RewardCount.text = $"x{itemBase.Amount}";
            try
            {
                var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[itemBase.SheetItemId];
                RewardGrade.sprite = SpriteHelper.GetItemBackground(itemSheetData.Grade);
                itemBaseForToolTip = ItemFactory.CreateItem(itemSheetData, new Cheat.DebugRandom());
            }
            catch
            {
                NcDebug.LogError($"Can't Find Item ID {itemBase.SheetItemId} in ItemSheet");
            }
            fungibleAssetValue = null;
        }
    }
}
