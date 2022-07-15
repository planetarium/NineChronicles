using System.Linq;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class WorldBossHelper
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

        public static bool TryGetBossPrefab(int bossId, out GameObject prefab)
        {
            var result = ScriptableObject.Monsters.FirstOrDefault(x => x.id == bossId);
            if (result is null)
            {
                prefab = null;
                return false;
            }

            prefab = result.prefab;
            return true;
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
