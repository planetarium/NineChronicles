using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
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

        public const int DefaultRuneId = 30001;
        public static Sprite DefaultRuneIcon => RuneData.DefaultRuneIcon;

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

        public static bool TryGetRuneData(string ticker, out RuneScriptableObject.RuneData data)
        {
            var result = RuneData.Runes.FirstOrDefault(x => x.ticker == ticker);
            if (result is null)
            {
                data = null;
                return false;
            }

            data = result;
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
                case 30001:
                case 20001:
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

        public static string GetRuneValueString(RuneOptionSheet.Row.RuneOptionInfo option)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (tableSheets.SkillActionBuffSheet.TryGetValue(option.SkillId,
                    out var skillActionBuffRow))
            {
                var buffIds = skillActionBuffRow.BuffIds;
                var isVampiric = tableSheets.ActionBuffSheet.Any(tuple =>
                    buffIds.Contains(tuple.Key) &&
                    tuple.Value.ActionBuffType == ActionBuffType.Vampiric);
                if (isVampiric)
                {
                    // value = 3400 -> "34%", 3444 -> "34.44%"
                    return $"{Math.Round(option.SkillValue / 100, 2)}%";
                }

                var isStun = tableSheets.ActionBuffSheet.Any(tuple =>
                    buffIds.Contains(tuple.Key) &&
                    tuple.Value.ActionBuffType == ActionBuffType.Stun);
                if (isStun)
                {
                    return "100%";
                }
            }

            var isPercent = option.SkillValueType == StatModifier.OperationType.Percentage;
            var curPower = isPercent ? option.SkillValue * 100 : option.SkillValue;
            string valueString;

            if (tableSheets.SkillSheet.TryGetValue(option.SkillId, out var skillRow) &&
                tableSheets.SkillBuffSheet.TryGetValue(skillRow.Id, out var skillBuffRow) &&
                tableSheets.StatBuffSheet.TryGetValue(skillBuffRow.BuffIds.First(), out var buffRow))
            {
                valueString = $"{buffRow.StatType.ValueToString((long)curPower)}%";
            }
            else if (isPercent)
            {
                valueString = $"{curPower}%";
            }
            else
            {
                valueString = curPower == (long)curPower ?
                    $"{(long)curPower}" : $"{curPower}";
            }

            return valueString;
        }

        public static int CalculateRuneLevelBonusReward(
            decimal bonusLevel,
            RuneLevelBonusSheet runeLevelBonusSheet)
        {
            bonusLevel /= 10000m;
            var bonusRow = runeLevelBonusSheet.Values
                .OrderByDescending(row => row.RuneLevel)
                .FirstOrDefault(row => row.RuneLevel <= bonusLevel);
            return bonusRow?.Bonus * (int)bonusLevel ?? 0;
        }

        public static decimal CalculateRuneLevelBonus(
            AllRuneState allRuneState,
            RuneListSheet runeListSheet)
        {
            return (from rune in allRuneState.Runes.Values
                let bonusCoef = runeListSheet[rune.RuneId].BonusCoef
                select bonusCoef * rune.Level).Sum();
        }

        public static decimal CalculateRuneLevelBonus(
            AllRuneState allRuneState,
            RuneListSheet runeListSheet,
            (int id, int level) editRune)
        {
            return (from rune in allRuneState.Runes.Values
                let bonusCoef = runeListSheet[rune.RuneId].BonusCoef
                let runeLevel = rune.RuneId == editRune.id ? editRune.level : rune.Level
                select bonusCoef * runeLevel).Sum();
        }
    }
}
