using System.Collections.Generic;
using System.Linq;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public static class StageSheetExtension
    {
        private static readonly Dictionary<int, List<MaterialItemSheet.Row>> GetRewardItemRowsCache = new Dictionary<int, List<MaterialItemSheet.Row>>();

        public static string GetLocalizedDescription(this StageWaveSheet.Row stageRow)
        {
            // todo: return L10nManager.Localize($"{stageRow.Key}");
            return $"{stageRow.Key}: Description";
        }

        public static List<MaterialItemSheet.Row> GetRewardItemRows(this StageSheet.Row stageRow)
        {
            if (GetRewardItemRowsCache.ContainsKey(stageRow.Key))
                return GetRewardItemRowsCache[stageRow.Key];

            var tableSheets = Game.Game.instance.TableSheets;
            var itemRows = new List<MaterialItemSheet.Row>();
            foreach (var itemId in stageRow.Rewards.Select(rewardData => rewardData.ItemId))
            {
                if (!tableSheets.MaterialItemSheet.TryGetValue(itemId, out var item))
                    continue;

                itemRows.Add(item);
            }

            var result = itemRows.Distinct().ToList();
            GetRewardItemRowsCache.Add(stageRow.Key, result);
            return result;
        }
    }
}
