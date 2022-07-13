using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class WorldBossState : IState
    {
        public int Id;
        public int Level;
        public int CurrentHP;
        public long StartedBlockIndex;
        public long EndedBlockIndex;

        public WorldBossState(WorldBossListSheet.Row row)
        {
            // Fenrir Id.
            Id = row.BossId;
            Level = 1;
            CurrentHP = 20_000;
            StartedBlockIndex = row.StartedBlockIndex;
            EndedBlockIndex = row.EndedBlockIndex;
        }

        public WorldBossState(List serialized)
        {
            Id = serialized[0].ToInteger();
            Level = serialized[1].ToInteger();
            CurrentHP = serialized[2].ToInteger();
            StartedBlockIndex = serialized[3].ToLong();
            EndedBlockIndex = serialized[4].ToLong();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Id.Serialize())
                .Add(Level.Serialize())
                .Add(CurrentHP.Serialize())
                .Add(StartedBlockIndex.Serialize())
                .Add(EndedBlockIndex.Serialize());
        }
    }
}
