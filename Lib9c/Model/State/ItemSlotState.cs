using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;

namespace Nekoyume.Model.State
{
    public class ItemSlotState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, BattleType battleType) =>
            avatarAddress.Derive($"item_slot_state_{battleType}");

        public BattleType BattleType { get; }
        public List<Guid> Costumes { get; }
        public List<Guid> Equipments { get; }

        public ItemSlotState(BattleType battleType)
        {
            BattleType = battleType;
            Costumes = new List<Guid>();
            Equipments = new List<Guid>();
        }

        public ItemSlotState(List serialized)
        {
            BattleType = serialized[0].ToEnum<BattleType>();
            Costumes = serialized[1].ToList(StateExtensions.ToGuid);
            Equipments = serialized[2].ToList(StateExtensions.ToGuid);
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(BattleType.Serialize())
                .Add(Costumes.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Equipments.OrderBy(x => x).Select(x => x.Serialize()).Serialize());
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
