using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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

        public static bool TryGetBossData(int bossId, out WorldBossScriptableObject.MonsterData data)
        {
            var result = ScriptableObject.Monsters.FirstOrDefault(x => x.id == bossId);
            if (result is null)
            {
                data = null;
                return false;
            }

            data = result;
            return true;
        }

        public static bool TryGetRaid(int raidId, out WorldBossListSheet.Row row)
        {
            var listSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            row = listSheet.Values.FirstOrDefault(x => x.Id.Equals(raidId));
            return row is not null;
        }

        public static bool TryGetRuneIcon(string ticker, out Sprite icon)
        {
            var result = ScriptableObject.Runes.FirstOrDefault(x => x.ticker == ticker);
            if (result is null)
            {
                icon = null;
                return false;
            }

            icon = result.icon;
            return true;
        }

        public static bool TryGetRunes(int bossId, out List<RuneSheet.Row> rows)
        {
            var runeIds = new List<int>();

            var runeWeightSheet = Game.Game.instance.TableSheets.RuneWeightSheet;
            var weightRows = runeWeightSheet.Values.Where(x => x.BossId == bossId).ToList();
            foreach (var row in weightRows)
            {
                runeIds.AddRange(row.RuneInfos.Select(x => x.RuneId));
            }

            runeIds.Distinct();

            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            rows = runeSheet.Values.Where(x => runeIds.Contains(x.Id)).ToList();
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
