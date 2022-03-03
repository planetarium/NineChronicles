using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Battle
{
    public readonly struct SimulationEnemyPlayer: IState
    {
        public readonly string NameWithHash;
        public readonly int CharacterId;
        public readonly int Level;
        public readonly int HairIndex;
        public readonly int LensIndex;
        public readonly int EarIndex;
        public readonly int TailIndex;

        public readonly IReadOnlyList<Costume> Costumes;
        public readonly IReadOnlyList<Equipment> Equipments;

        public SimulationEnemyPlayer(EnemyPlayer enemyPlayer)
        {
            NameWithHash = enemyPlayer.NameWithHash;
            CharacterId = enemyPlayer.CharacterId;
            Level = enemyPlayer.Level;
            HairIndex = enemyPlayer.hairIndex;
            LensIndex = enemyPlayer.hairIndex;
            EarIndex = enemyPlayer.earIndex;
            TailIndex = enemyPlayer.tailIndex;
            Costumes = enemyPlayer.Costumes;
            Equipments = enemyPlayer.Equipments;
        }

        public SimulationEnemyPlayer(List serialized)
        {
            NameWithHash = serialized[0].ToDotnetString();
            CharacterId = serialized[1].ToInteger();
            Level = serialized[2].ToInteger();
            HairIndex = serialized[3].ToInteger();
            LensIndex = serialized[4].ToInteger();
            EarIndex = serialized[5].ToInteger();
            TailIndex = serialized[6].ToInteger();
            Costumes = ((List) serialized[7]).Select(c =>
                (Costume) ItemFactory.Deserialize((Dictionary) c)).ToList();
            Equipments = ((List) serialized[8]).Select(e =>
                (Equipment) ItemFactory.Deserialize((Dictionary) e)).ToList();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(NameWithHash.Serialize())
                .Add(CharacterId.Serialize())
                .Add(Level.Serialize())
                .Add(HairIndex.Serialize())
                .Add(LensIndex.Serialize())
                .Add(EarIndex.Serialize())
                .Add(TailIndex.Serialize())
                .Add(Costumes.Aggregate(List.Empty, (current, costume) => current.Add(costume.Serialize())))
                .Add(Equipments.Aggregate(List.Empty, (current, equipment) => current.Add(equipment.Serialize())));
        }
    }
}
