using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.State;
using System.Text.RegularExpressions;

namespace Nekoyume.ActionExtensions
{
    public static class AdventureBossExtensions
    {
        public static string GetParsedName(this Investor investor)
        {
            string pattern = "\"([^\"]+)\"";
            Match match = Regex.Match(investor.Name, pattern);
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
    }
}
