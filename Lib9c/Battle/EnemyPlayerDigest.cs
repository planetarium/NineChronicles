using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Battle
{
    public readonly struct EnemyPlayerDigest: IState
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

        public EnemyPlayerDigest(AvatarState enemyAvatarState)
        {
            NameWithHash = enemyAvatarState.NameWithHash;
            CharacterId = enemyAvatarState.characterId;
            Level = enemyAvatarState.level;
            HairIndex = enemyAvatarState.hair;
            LensIndex = enemyAvatarState.lens;
            EarIndex = enemyAvatarState.ear;
            TailIndex = enemyAvatarState.tail;
            Costumes = enemyAvatarState.inventory.Costumes.Where(c => c.equipped).ToList();
            Equipments = enemyAvatarState.inventory.Equipments.Where(e => e.equipped).ToList();
        }

        public EnemyPlayerDigest(List serialized)
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
