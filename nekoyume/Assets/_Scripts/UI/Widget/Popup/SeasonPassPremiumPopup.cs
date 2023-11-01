using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Nekoyume.UI
{
    public class SeasonPassPremiumPopup : PopupWidget
    {
        [SerializeField]
        private GameObject[] isPremiumObj;
        [SerializeField]
        private GameObject[] notPremiumObj;

        [SerializeField]
        private GameObject[] isPremiumPlusObj;
        [SerializeField]
        private GameObject[] notPremiumPlusObj;

        [SerializeField]
        private GameObject premiumPurchaseButtonDisabledObj;
        [SerializeField]
        private GameObject premiumPurchaseButtonPriceObj;
        [SerializeField]
        private GameObject premiumPurchaseButtonLoadingObj;


        [SerializeField]
        private GameObject premiumPlusPurchaseButtonDisabledObj;
        [SerializeField]
        private GameObject premiumPlusPurchaseButtonPriceObj;
        [SerializeField]
        private GameObject premiumPlusPurchaseButtonLoadingObj;

        protected override void Awake()
        {
            base.Awake();
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarInfo.Subscribe((seasonPassInfo) => {
                if (seasonPassInfo == null)
                    return;

                foreach (var item in isPremiumObj)
                {
                    item.SetActive(seasonPassInfo.IsPremium);
                }
                foreach (var item in notPremiumObj)
                {
                    item.SetActive(!seasonPassInfo.IsPremium);
                }
                premiumPurchaseButtonPriceObj.SetActive(seasonPassInfo.IsPremium);

                foreach (var item in isPremiumPlusObj)
                {
                    item.SetActive(seasonPassInfo.IsPremiumPlus);
                }
                foreach (var item in notPremiumPlusObj)
                {
                    item.SetActive(!seasonPassInfo.IsPremiumPlus);
                }
                premiumPlusPurchaseButtonPriceObj.SetActive(seasonPassInfo.IsPremiumPlus);

            }).AddTo(gameObject);
        }

        public void PurchaseSeasonPassPremiumButton()
        {
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            if (seasonPassManager.AvatarInfo.Value.IsPremium)
                return;

            if (Game.Game.instance.IAPStoreManager.SeasonPassProduct.TryGetValue("key", out var product))
            {
                premiumPurchaseButtonDisabledObj.SetActive(true);
                premiumPurchaseButtonPriceObj.SetActive(false);
                premiumPurchaseButtonLoadingObj.SetActive(true);
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked(product.GoogleSku);
            }
        }

        public void PurchaseSeasonPassPremiumPlusButton()
        {
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            if (seasonPassManager.AvatarInfo.Value.IsPremiumPlus)
                return;

            if (Game.Game.instance.IAPStoreManager.SeasonPassProduct.TryGetValue("key", out var product))
            {
                premiumPlusPurchaseButtonDisabledObj.SetActive(true);
                premiumPlusPurchaseButtonPriceObj.SetActive(false);
                premiumPlusPurchaseButtonLoadingObj.SetActive(true);
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked(product.GoogleSku);
            }

        }

        public void PurchaseButtonLoadingEnd()
        {
            premiumPurchaseButtonLoadingObj.SetActive(false);
            premiumPlusPurchaseButtonLoadingObj.SetActive(false);
        }
    }
}
