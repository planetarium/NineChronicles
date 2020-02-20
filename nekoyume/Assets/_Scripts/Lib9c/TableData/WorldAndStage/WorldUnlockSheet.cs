using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldUnlockSheet : Sheet<int, WorldUnlockSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int WorldId { get; private set; }
            public int StageId { get; private set; }
            public int WorldIdToUnlock { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = string.IsNullOrEmpty(fields[0])
                    ? throw new SheetRowColumnException(nameof(Id))
                    : int.Parse(fields[0]);
                WorldId = string.IsNullOrEmpty(fields[1])
                    ? throw new SheetRowColumnException(nameof(WorldId))
                    : int.Parse(fields[1]);
                StageId = string.IsNullOrEmpty(fields[2])
                    ? throw new SheetRowColumnException(nameof(StageId))
                    : int.Parse(fields[2]);
                WorldIdToUnlock = string.IsNullOrEmpty(fields[3])
                    ? throw new SheetRowColumnException(nameof(WorldIdToUnlock))
                    : int.Parse(fields[3]);
            }
        }

        public WorldUnlockSheet() : base(nameof(WorldUnlockSheet))
        {
        }

        public bool TryGetUnlockedInformation(int clearedWorldId, int clearedStageId, out List<int> worldIdsToUnlock)
        {
            worldIdsToUnlock = Values
                .Where(e => e.WorldId == clearedWorldId && e.StageId == clearedStageId)
                .Select(value => value.WorldIdToUnlock)
                .ToList();

            return worldIdsToUnlock.Count > 0;
        }
    }
}
