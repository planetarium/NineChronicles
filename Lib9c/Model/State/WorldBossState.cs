using System;
using System.Numerics;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class WorldBossState : IState
    {
        public int Id;
        public int Level;
        public BigInteger CurrentHp;
        public long StartedBlockIndex;
        public long EndedBlockIndex;

        public WorldBossState(WorldBossListSheet.Row row, WorldBossGlobalHpSheet.Row hpRow)
        {
            // Fenrir Id.
            Id = row.BossId;
            Level = 1;
            CurrentHp = hpRow.Hp;
            StartedBlockIndex = row.StartedBlockIndex;
            EndedBlockIndex = row.EndedBlockIndex;
        }

        public WorldBossState(List serialized)
        {
            Id = serialized[0].ToInteger();
            Level = serialized[1].ToInteger();
            CurrentHp = serialized[2].ToBigInteger();
            StartedBlockIndex = serialized[3].ToLong();
            EndedBlockIndex = serialized[4].ToLong();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Id.Serialize())
                .Add(Level.Serialize())
                .Add(CurrentHp.Serialize())
                .Add(StartedBlockIndex.Serialize())
                .Add(EndedBlockIndex.Serialize());
        }
    }
}
