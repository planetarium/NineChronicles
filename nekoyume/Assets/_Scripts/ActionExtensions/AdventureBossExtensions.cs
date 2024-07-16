using Lib9c;
using Nekoyume.Model.AdventureBoss;
using System.Text.RegularExpressions;

namespace Nekoyume.ActionExtensions
{
    public static class AdventureBossExtensions
    {
        public static string GetParsedName(this Investor investor)
        {
            var pattern = "\"([^\"]+)\"";
            var match = Regex.Match(investor.Name, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                NcDebug.LogError($"[AdventureBossData.GetParsedName] Failed to parse name: {investor.Name}");
                return investor.Name;
            }
        }

        public static bool ItemViewSetAdventureBossItemData(this BaseItemView baseItemView, TableData.AdventureBoss.AdventureBossSheet.RewardAmountData reward)
        {
            baseItemView.gameObject.SetActive(true);
            switch (reward.ItemType)
            {
                case "Material":
                    baseItemView.ItemViewSetItemData(reward.ItemId, reward.Amount);
                    return true;
                case "Rune":
                    baseItemView.ItemViewSetCurrencyData(reward.ItemId, reward.Amount);
                    return true;
                case "Crystal":
                    baseItemView.ItemViewSetCurrencyData(Currencies.Crystal.Ticker, reward.Amount);
                    return true;
                default:
                    NcDebug.LogError($"Invalid ItemType: {reward.ItemType} ItemId: {reward.ItemId}");
                    baseItemView.gameObject.SetActive(false);
                    return false;
            }
        }

        public static bool ItemViewSetAdventureBossItemData(this BaseItemView baseItemView, TableData.AdventureBoss.AdventureBossSheet.RewardRatioData reward)
        {
            baseItemView.gameObject.SetActive(true);
            switch (reward.ItemType)
            {
                case "Material":
                    baseItemView.ItemViewSetItemData(reward.ItemId, 0);
                    return true;
                case "Rune":
                    baseItemView.ItemViewSetCurrencyData(reward.ItemId, 0);
                    return true;
                case "Crystal":
                    baseItemView.ItemViewSetCurrencyData(Currencies.Crystal.Ticker, 0);
                    return true;
                default:
                    NcDebug.LogError($"Invalid ItemType: {reward.ItemType} ItemId: {reward.ItemId}");
                    baseItemView.gameObject.SetActive(false);
                    return false;
            }
        }
    }
}
