using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1156
    /// </summary>
    public class ArenaAvatarState : IState
    {
        public static Address DeriveAddress(Address avatarAddress) =>
            avatarAddress.Derive("arena_avatar");

        public Address Address;
        public List<Guid> Costumes { get; }
        public List<Guid> Equipments { get; }

        public long LastBattleBlockIndex;

        public ArenaAvatarState(AvatarState avatarState)
        {
            Address = DeriveAddress(avatarState.address);
            Costumes = new List<Guid>();
            Equipments = new List<Guid>();
            LastBattleBlockIndex = 0;
        }

        public ArenaAvatarState(List serialized)
        {
            Address = serialized[0].ToAddress();
            Costumes = serialized[1].ToList(StateExtensions.ToGuid);
            Equipments = serialized[2].ToList(StateExtensions.ToGuid);
            LastBattleBlockIndex = serialized.Count > 3 ? serialized[3].ToLong() : 0;
        }

        public IValue Serialize()
        {
            var result = List.Empty
                .Add(Address.Serialize())
                .Add(Costumes.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Equipments.OrderBy(x => x).Select(x => x.Serialize()).Serialize());
            if (LastBattleBlockIndex != 0)
            {
                result = result.Add(LastBattleBlockIndex.Serialize());
            }

            return result;
        }

        public void UpdateCostumes(List<Guid> costumes)
        {
            if (costumes == null)
            {
                throw new ArgumentNullException(nameof(costumes));
            }

            Costumes.Clear();
            Costumes.AddRange(costumes);
        }

        public void UpdateEquipment(List<Guid> equipments)
        {
            if (equipments == null)
            {
                throw new ArgumentNullException(nameof(equipments));
            }

            Equipments.Clear();
            Equipments.AddRange(equipments);
        }
    }
}
