using System;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class WorldBossFrontHelper
    {
        private static WorldBossScriptableObject _scriptableObject;

        private static WorldBossScriptableObject ScriptableObject
        {
            get
            {
                if (_scriptableObject == null)
                {
                    _scriptableObject = Resources.Load<WorldBossScriptableObject>(
                        "ScriptableObject/UI_WorldBossData");
                }

                return _scriptableObject;
            }
        }

        public static bool TryGetGrade(WorldBossGrade grade, out GameObject prefab)
        {
            var result = ScriptableObject.Grades.FirstOrDefault(x => x.grade == grade);
            if (result is null)
            {
                prefab = null;
                return false;
            }

            prefab = result.prefab;
            return true;
        }

        public static bool TryGetBossPrefab(int bossId, out GameObject namePrefab, out GameObject spinePrefab)
        {
            var result = ScriptableObject.Monsters.FirstOrDefault(x => x.id == bossId);
            if (result is null)
            {
                namePrefab = null;
                spinePrefab = null;
                return false;
            }

            namePrefab = result.namePrefab;
            spinePrefab = result.spinePrefab;
            return true;
        }

        public static bool TryGetBossName(int bossId, out string name)
        {
            var result = ScriptableObject.Monsters.FirstOrDefault(x => x.id == bossId);
            if (result is null)
            {
                name = string.Empty;
                return false;
            }

            name = result.name;
            return true;
        }

        public static bool TryGetRuneIcon(Currency currency, out Sprite icon)
        {
            var ticker = currency.Ticker;
            var currencyId = Convert.ToInt32(ticker);
            var result = ScriptableObject.Runes.FirstOrDefault(x => x.id == currencyId);
            if (result is null)
            {
                icon = null;
                return false;
            }

            icon = result.icon;
            return true;
        }

        public static bool IsItInSeason(long currentBlockIndex)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            return sheet.Values.Any(x => x.StartedBlockIndex <= currentBlockIndex &&
                                                   currentBlockIndex <= x.EndedBlockIndex);
        }

        public static bool TryGetCurrentRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            row = sheet.Values.FirstOrDefault(x => x.StartedBlockIndex <= currentBlockIndex &&
                                             currentBlockIndex <= x.EndedBlockIndex);
            return row is not null;
        }

        public static bool TryGetPreviousRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var rows = sheet.Values.Where(x => x.EndedBlockIndex < currentBlockIndex)
                .OrderByDescending(x => x.EndedBlockIndex)
                .ToList();
            row = rows.Any() ? rows.First() : null;
            return rows.Any();
        }

        public static bool TryGetNextRow(long currentBlockIndex, out WorldBossListSheet.Row row)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var rows = sheet.Values.Where(x => x.StartedBlockIndex > currentBlockIndex)
                                            .OrderBy(x => x.StartedBlockIndex)
                                            .ToList();
            row = rows.Any() ? rows.First() : null;
            return rows.Any();
        }
    }
}
