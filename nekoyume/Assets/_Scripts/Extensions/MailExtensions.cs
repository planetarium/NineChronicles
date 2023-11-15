using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using NineChronicles.ExternalServices.IAPService.Runtime;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;

namespace Nekoyume
{
    public static class MailExtensions
    {
        public static async Task<string> GetCellContentAsync(this UnloadFromMyGaragesRecipientMail mail)
        {
            if(mail.Memo != null && mail.Memo.Contains("season_pass"))
            {
                if(mail.Memo.Contains("\"t\": \"auto\""))
                {
                    return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS_ENDED");
                }
                else
                {
                    return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS");
                }
            }

            var game = Game.Game.instance;
            var iapServiceManager = game.IAPServiceManager;
            if (iapServiceManager is null)
            {
                Debug.Log($"{nameof(IAPServiceManager)} is null.");
                return mail.GetCellContentsForException();
            }

            var agentAddr = game.Agent.Address;

            var categorys = await iapServiceManager.GetProductsAsync(agentAddr);
            List<ProductSchema> products = new List<ProductSchema>();
            foreach (var catagory in categorys)
            {
                products.AddRange(catagory.ProductList);
            }

            if (products is null)
            {
                Debug.Log("products is null.");
                return mail.GetCellContentsForException();
            }

            var product = products.FirstOrDefault(p =>
            {
                if (p.FavList.Any())
                {
                    if (mail.FungibleAssetValues is null)
                    {
                        return false;
                    }

                    // NOTE: Here we compare `prodFav.Amount` to `mailFavTuple.value.MajorUnit`.
                    //       Because the type of `prodFav.Amount` is `int`. When the type of
                    //       `prodFav.Amount` to be `decimal` or, `prodFav` provides `MajorUnit`
                    //       and `MinorUnit`, we should change this code.
                    if (!mail.FungibleAssetValues.All(mFavTup =>
                            p.FavList.Any(prodFav =>
                                prodFav.Ticker.ToString() == mFavTup.value.Currency.Ticker &&
                                prodFav.Amount == (decimal)mFavTup.value.MajorUnit)))
                    {
                        return false;
                    }
                }

                if (p.FungibleItemList.Any())
                {
                    if (mail.FungibleIdAndCounts is null)
                    {
                        return false;
                    }

                    if (!mail.FungibleIdAndCounts.All(mFItemTup =>
                            p.FungibleItemList.Any(prodFItem =>
                                prodFItem.FungibleItemId == mFItemTup.fungibleId.ToString() &&
                                prodFItem.Amount == mFItemTup.count)))
                    {
                        return false;
                    }
                }

                return true;
            });
            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var iapStoreManager = game.IAPStoreManager;
            if (iapStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            var storeProduct = iapStoreManager.IAPProducts.FirstOrDefault(p =>
                p.definition.id == product.GoogleSku);
            if (storeProduct is null)
            {
                return mail.GetCellContentsForException();
            }

            var format = L10nManager.Localize(
                "MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_CELL_CONTENT_FORMAT");
            return string.Format(format, storeProduct.metadata.localizedTitle);
        }

        private static string GetCellContentsForException(
            this UnloadFromMyGaragesRecipientMail mail)
        {
            var exceptionFormat = L10nManager.Localize(
                "MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_CELL_CONTENT_EXCEPTION_FORMAT");
            return string.Format(
                exceptionFormat,
                mail.FungibleAssetValues?.Count() ?? 0,
                mail.FungibleIdAndCounts?.Count() ?? 0);
        }

        public static async Task<string> GetCellContentAsync(this ClaimItemsMail mail)
        {
            if (mail.Memo != null && mail.Memo.Contains("season_pass"))
            {
                if (mail.Memo.Contains("\"t\": \"auto\""))
                {
                    return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS_ENDED");
                }

                return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS");
            }

            if (mail.Memo != null && mail.Memo.Contains("patrol"))
            {
                return L10nManager.Localize("NOTIFICATION_PATROL_REWARD_CLAIMED");
            }

            var game = Game.Game.instance;
            var iapServiceManager = game.IAPServiceManager;
            if (iapServiceManager is null)
            {
                Debug.Log($"{nameof(IAPServiceManager)} is null.");
                return mail.GetCellContentsForException();
            }

            var agentAddr = game.Agent.Address;

            var categoryList = await iapServiceManager.GetProductsAsync(agentAddr);
            List<ProductSchema> products = new List<ProductSchema>();
            if (categoryList is null)
            {
                Debug.Log("products is null.");
                return mail.GetCellContentsForException();
            }

            foreach (var category in categoryList)
            {
                products.AddRange(category.ProductList);
            }

            var product = products.FirstOrDefault(p =>
            {
                if (p.FavList.Any())
                {
                    if (mail.FungibleAssetValues is null)
                    {
                        return false;
                    }

                    // NOTE: Here we compare `prodFav.Amount` to `mailFavTuple.value.MajorUnit`.
                    //       Because the type of `prodFav.Amount` is `int`. When the type of
                    //       `prodFav.Amount` to be `decimal` or, `prodFav` provides `MajorUnit`
                    //       and `MinorUnit`, we should change this code.
                    if (!mail.FungibleAssetValues.All(fav =>
                            p.FavList.Any(prodFav =>
                                prodFav.Ticker.ToString() == fav.Currency.Ticker &&
                                prodFav.Amount == (decimal)fav.MajorUnit)))
                    {
                        return false;
                    }
                }

                if (p.FungibleItemList.Any())
                {
                    if (mail.Items is null)
                    {
                        return false;
                    }

                    if (!mail.Items.All(itemTuple =>
                            p.FungibleItemList.Any(prodFItem =>
                                prodFItem.SheetItemId == itemTuple.id &&
                                prodFItem.Amount == itemTuple.count)))
                    {
                        return false;
                    }
                }

                return true;
            });
            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var iapStoreManager = game.IAPStoreManager;
            if (iapStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            var storeProduct = iapStoreManager.IAPProducts.FirstOrDefault(p =>
                p.definition.id == product.GoogleSku);
            if (storeProduct is null)
            {
                return mail.GetCellContentsForException();
            }

            var format = L10nManager.Localize(
                "MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_CELL_CONTENT_FORMAT");
            return string.Format(format, storeProduct.metadata.localizedTitle);
        }

        private static string GetCellContentsForException(
            this ClaimItemsMail mail)
        {
            var exceptionFormat = L10nManager.Localize(
                "MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_CELL_CONTENT_EXCEPTION_FORMAT");
            return string.Format(
                exceptionFormat,
                mail.FungibleAssetValues?.Count ?? 0,
                mail.Items?.Count ?? 0);
        }
    }
}
