using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nekoyume.Blockchain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
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
                NcDebug.Log($"{nameof(IAPServiceManager)} is null.");
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
            string itemNames = string.Empty;
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
                    if(itemRow != null)
                    {
                        var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                        itemNames += LocalizationExtensions.GetLocalizedName(item) + ", ";
                        continue;
                    }
                }
            }

            string stringToRemove = ", ";
            if (itemNames.EndsWith(stringToRemove))
            {
                itemNames = itemNames.Substring(0, itemNames.Length - stringToRemove.Length);
            }

            var exceptionFormat = L10nManager.Localize(
                "UI_RECEIVED");
            itemNames += " "+ exceptionFormat;

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

            var game = Game.Game.instance;
            var iapServiceManager = game.IAPServiceManager;
            if (iapServiceManager is null)
            {
                NcDebug.Log($"{nameof(IAPServiceManager)} is null.");
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
            string itemNames = string.Empty;
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

            string stringToRemove = ", ";
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
