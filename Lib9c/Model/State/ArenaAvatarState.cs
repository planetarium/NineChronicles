using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1027
    /// </summary>
    public class ArenaAvatarState : IState
    {
        public static Address DeriveAddress(Address avatarAddress) =>
            avatarAddress.Derive("arena_avatar");

        public Address Address;
        public List<Guid> Costumes { get; }
        public List<Guid> Equipments { get; }

        /// It is only for previewnet.
        public int Level { get; private set; }

        public ArenaAvatarState(AvatarState avatarState)
        {
            Address = DeriveAddress(avatarState.address);
            Costumes = new List<Guid>();
            Equipments = new List<Guid>();
            Level = avatarState.level;
        }

        public ArenaAvatarState(List serialized)
        {
            Address = serialized[0].ToAddress();
            Costumes = serialized[1].ToList(StateExtensions.ToGuid);
            Equipments = serialized[2].ToList(StateExtensions.ToGuid);
            Level = (Integer)serialized[3];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Costumes.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Equipments.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Level);
        }

        public void UpdateCostumes([NotNull] List<Guid> costumes)
        {
            if (costumes == null)
            {
                throw new ArgumentNullException(nameof(costumes));
            }

            Costumes.Clear();
            Costumes.AddRange(costumes);
        }

        public void UpdateEquipment([NotNull] List<Guid> equipments)
        {
            if (equipments == null)
            {
                throw new ArgumentNullException(nameof(equipments));
            }

            Equipments.Clear();
            Equipments.AddRange(equipments);
        }

        /// It is only for previewnet.
        public void UpdateLevel(int level)
        {
            Level = level;
        }
    }
}
