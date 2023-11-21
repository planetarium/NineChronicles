using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            if (mail.Memo is null)
            {
                return mail.GetCellContentsForException();
            }

            if (mail.Memo != null && mail.Memo.Contains("season_pass"))
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

            if(Game.Game.instance.IAPStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            var agentAddr = game.Agent.Address;

            ProductSchema product = null;
            if (mail.Memo.Contains("iap"))
            {
                product = GetProductFromMemo(mail.Memo);
            }

            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var productName = L10nManager.Localize(product.L10n_Key);

            var format = L10nManager.Localize(
                "UI_IAP_PURCHASE_DELIVERY_COMPLETE_MAIL");
            return string.Format(format, productName);
        }

        public static ProductSchema GetProductFromMemo(string memo)
        {
            ProductSchema product = null;
#if UNITY_IOS
                Regex gSkuRegex = new Regex("\"a_sku\": \"([^\"]+)\"");
#else
            Regex gSkuRegex = new Regex("\"g_sku\": \"([^\"]+)\"");
#endif
            Match gSkuMatch = gSkuRegex.Match(memo);
            if (gSkuMatch.Success)
            {
                product = Game.Game.instance.IAPStoreManager.GetProductSchema(gSkuMatch.Groups[1].Value);
            }

            return product;
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
            if (mail.Memo is null)
            {
                return mail.GetCellContentsForException();
            }

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

            if (Game.Game.instance.IAPStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            var agentAddr = game.Agent.Address;

            ProductSchema product = null;
            if (mail.Memo.Contains("iap"))
            {
#if UNITY_IOS
                Regex gSkuRegex = new Regex("\"a_sku\": \"([^\"]+)\"");
#else
                Regex gSkuRegex = new Regex("\"g_sku\": \"([^\"]+)\"");
#endif
                Match gSkuMatch = gSkuRegex.Match(mail.Memo);
                if (gSkuMatch.Success)
                {
                    product = Game.Game.instance.IAPStoreManager.GetProductSchema(gSkuMatch.Groups[1].Value);
                }
            }

            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var productName = L10nManager.Localize(product.L10n_Key);

            var format = L10nManager.Localize(
                "UI_IAP_PURCHASE_DELIVERY_COMPLETE_MAIL");
            return string.Format(format, productName);
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
