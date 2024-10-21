using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
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
                if (mail.Memo.Contains("\"t\": \"auto\""))
                {
                    return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS_ENDED");
                }
                else
                {
                    return L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_SEASON_PASS");
                }
            }

            ;
            var iapServiceManager = ApiClients.Instance.IAPServiceManager;
            if (iapServiceManager is null)
            {
                NcDebug.Log($"{nameof(IAPServiceManager)} is null.");
                return mail.GetCellContentsForException();
            }

            if (Game.Game.instance.IAPStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            InAppPurchaseServiceClient.ProductSchema product = null;
            if (mail.Memo.Contains("iap"))
            {
                product = GetProductFromMemo(mail.Memo);
            }

            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var productName = L10nManager.Localize(product.L10nKey);

            var format = L10nManager.Localize(
                "UI_IAP_PURCHASE_DELIVERY_COMPLETE_MAIL");
            return string.Format(format, productName);
        }

        public static string GetSkuFromMemo(string memo)
        {
            string sku = string.Empty;
#if UNITY_IOS
                Regex gSkuRegex = new Regex("\"a_sku\": \"([^\"]+)\"");
#else
            var gSkuRegex = new Regex("\"g_sku\": \"([^\"]+)\"");
#endif
            var gSkuMatch = gSkuRegex.Match(memo);
            if (gSkuMatch.Success)
            {
                sku = gSkuMatch.Groups[1].Value;
            }
            return sku;
        }

        public static InAppPurchaseServiceClient.ProductSchema GetProductFromMemo(string memo)
        {
            InAppPurchaseServiceClient.ProductSchema product = null;
            var sku = GetSkuFromMemo(memo);
    
            if (!string.IsNullOrEmpty(sku))
            {
                product = Game.Game.instance.IAPStoreManager.GetProductSchema(sku);
            }

            return product;
        }

        private static string GetCellContentsForException(
            this UnloadFromMyGaragesRecipientMail mail)
        {
            var itemNames = string.Empty;
            if (mail.FungibleAssetValues is not null)
            {
                foreach (var fav in mail.FungibleAssetValues)
                {
                    itemNames += fav.value.GetLocalizedName() + ", ";
                }
            }

            if (mail.FungibleIdAndCounts is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var (fungibleId, count) in
                    mail.FungibleIdAndCounts)
                {
                    var row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.Id.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        itemNames += LocalizationExtensions.GetLocalizedName(material) + ", ";
                        continue;
                    }

                    NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");

                    row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        itemNames += LocalizationExtensions.GetLocalizedName(material) + ", ";
                        continue;
                    }

                    var itemRow = itemSheet.OrderedList!.FirstOrDefault(row => row.Equals(fungibleId));
                    if (itemRow != null)
                    {
                        var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                        itemNames += LocalizationExtensions.GetLocalizedName(item) + ", ";
                        continue;
                    }
                }
            }

            var stringToRemove = ", ";
            if (itemNames.EndsWith(stringToRemove))
            {
                itemNames = itemNames.Substring(0, itemNames.Length - stringToRemove.Length);
            }

            var exceptionFormat = L10nManager.Localize(
                "UI_RECEIVED");
            itemNames += " " + exceptionFormat;

            return itemNames;
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

            var iapServiceManager = ApiClients.Instance.IAPServiceManager;
            if (iapServiceManager is null)
            {
                NcDebug.Log($"{nameof(IAPServiceManager)} is null.");
                return mail.GetCellContentsForException();
            }

            if (Game.Game.instance.IAPStoreManager is null)
            {
                return mail.GetCellContentsForException();
            }

            var game = Game.Game.instance;
            var agentAddr = game.Agent.Address;

            InAppPurchaseServiceClient.ProductSchema product = null;
            if (mail.Memo.Contains("iap"))
            {
                product = GetProductFromMemo(mail.Memo);
            }

            if (product is null)
            {
                return mail.GetCellContentsForException();
            }

            var productName = L10nManager.Localize(product.L10nKey);

            var format = L10nManager.Localize(
                "UI_IAP_PURCHASE_DELIVERY_COMPLETE_MAIL");
            return string.Format(format, productName);
        }

        private static string GetCellContentsForException(
            this ClaimItemsMail mail)
        {
            var itemNames = string.Empty;
            if (mail.FungibleAssetValues is not null)
            {
                foreach (var fav in mail.FungibleAssetValues)
                {
                    itemNames += fav.GetLocalizedName() + ", ";
                }
            }

            if (mail.Items is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var (fungibleId, count) in
                    mail.Items)
                {
                    var row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.Id.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        itemNames += LocalizationExtensions.GetLocalizedName(material) + ", ";
                        continue;
                    }

                    row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        itemNames += LocalizationExtensions.GetLocalizedName(material) + ", ";
                        continue;
                    }

                    if (itemSheet.TryGetValue(fungibleId, out var itemSheetRow))
                    {
                        var item = ItemFactory.CreateItem(itemSheetRow, new ActionRenderHandler.LocalRandom(0));
                        itemNames += LocalizationExtensions.GetLocalizedName(item) + ", ";
                        continue;
                    }

                    NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                }
            }

            var stringToRemove = ", ";
            if (itemNames.EndsWith(stringToRemove))
            {
                itemNames = itemNames.Substring(0, itemNames.Length - stringToRemove.Length);
            }

            var exceptionFormat = L10nManager.Localize(
                "UI_RECEIVED");

            itemNames += " " + exceptionFormat;

            return itemNames;
        }
    }
}
