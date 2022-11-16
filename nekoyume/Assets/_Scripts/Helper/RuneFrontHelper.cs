using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;


namespace Nekoyume.Helper
{
    public static class RuneFrontHelper
    {
        private static RuneScriptableObject _runeData;

        private static RuneScriptableObject RuneData
        {
            get
            {
                if (_runeData == null)
                {
                    _runeData = Resources.Load<RuneScriptableObject>(
                        "ScriptableObject/UI_RuneData");
                }

                return _runeData;
            }
        }

        public static Sprite DefaultRuneIcon => RuneData.DefaultRuneIcon;

        public static int GetGroupId(int id)
        {
            var result = RuneData.Runes.FirstOrDefault(x => x.id == id);
            return result?.groupdId ?? 0;
        }

        public static string GetGroupName(int id)
        {
            return id < RuneData.GroupNames.Count
                ? L10nManager.Localize(RuneData.GroupNames[id])
                : string.Empty;
        }

        public static bool TryGetRuneIcon(int id, out Sprite icon)
        {
            var result = RuneData.Runes.FirstOrDefault(x => x.id == id);
            if (result is null)
            {
                icon = null;
                return false;
            }

            icon = result.icon;
            return true;
        }

        public static bool TryGetRuneStoneIcon(string ticker, out Sprite icon)
        {
            var result = RuneData.Runes.FirstOrDefault(x => x.ticker == ticker);
            if (result is null)
            {
                icon = null;
                return false;
            }

            icon = result.icon;
            return true;
        }

        public static bool TryGetRuneStoneIcon(int id, out Sprite icon)
        {
            var result = RuneData.Runes.FirstOrDefault(x => x.id == id);
            if (result is null)
            {
                icon = null;
                return false;
            }

            icon = result.icon;
            return true;
        }

        private static bool CanObtain(long currentBlockIndex, int runeStoneId)
        {
            if (WorldBossFrontHelper.IsItInSeason(currentBlockIndex))
            {
                if (WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var worldBossRow))
                {
                    if (WorldBossFrontHelper.TryGetRunes(worldBossRow.BossId, out var runeRows))
                    {
                        return runeRows.Exists(x => x.Id == runeStoneId);
                    }
                }
            }

            return false;
        }

        public static bool TryGetRunStoneInformation(
        long currentBlockIndex,
        int runeStoneId,
        out string info,
        out bool canObtain)
        {
            switch (runeStoneId)
            {
                case 3001:
                case 2001:
                    info = string.Empty;
                    canObtain = false;
                    return false;
                default:
                    canObtain = CanObtain(currentBlockIndex, runeStoneId);
                    if (canObtain)
                    {
                        info = L10nManager.Localize("UI_INFO_ON_SEASON_AVAILABLE");
                    }
                    else
                    {
                        info = WorldBossFrontHelper.IsItInSeason(currentBlockIndex)
                            ? L10nManager.Localize("UI_INFO_ON_SEASON_NOT_OBTAINED")
                            : L10nManager.Localize("UI_INFO_PRACTICE_MODE");
                    }
                    return true;
            }
        }
    }
}
