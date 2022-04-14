using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("grinding")]
    public class Grinding : GameAction
    {
        public const int CostAp = 5;
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            IAccountStateDelta states = ctx.PreviousStates;
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                states = EquipmentIds.Aggregate(states,
                    (current, guid) =>
                        current.SetState(Addresses.GetItemAddress(guid), MarkChanged));
                return states
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 0), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 1), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 2), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 3), MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, AvatarAddress);
            }

            if (!states.TryGetAgentAvatarStatesV2(ctx.Signer, AvatarAddress, out var agentState,
                    out var avatarState, out bool migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                context.Signer,
                agentState.MonsterCollectionRound
            );

            int monsterCollectionLevel = 0;
            if (states.TryGetState(monsterCollectionAddress, out Dictionary mcDict))
            {
                var monsterCollectionState = new MonsterCollectionState(mcDict);
                monsterCollectionLevel = monsterCollectionState.Level;
            }

            if (avatarState.actionPoint < CostAp)
            {
                throw new NotEnoughActionPointException("");
            }

            avatarState.actionPoint -= CostAp;

            var currency = new Currency("CRYSTAL", 2, minters: null);
            int cost = 0;
            foreach (var equipmentId in EquipmentIds)
            {
                int baseCost = 1000;
                if(avatarState.inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment))
                {
                    if (equipment.RequiredBlockIndex > context.BlockIndex)
                    {
                        throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                    }

                    if (equipment.equipped)
                    {
                        throw new InvalidEquipmentException($"Can't grind equipped item. {equipmentId}");
                    }

                    if (equipment.level > 0)
                    {
                        // TODO Different multiplier by sheet data.
                        baseCost *= equipment.level;
                    }
                }
                else
                {
                    // Invalid Item Type.
                    throw new ItemDoesNotExistException($"Can't find Equipment. {equipmentId}");
                }

                if (!avatarState.inventory.RemoveNonFungibleItem(equipmentId))
                {
                    throw new ItemDoesNotExistException($"Can't find Equipment. {equipmentId}");
                }

                cost += baseCost;
            }

            if (monsterCollectionLevel > 0)
            {
                // TODO Different multiplier by sheet data.
                cost *= monsterCollectionLevel;
            }

            var mail = new GrindingMail(
                ctx.BlockIndex,
                Id,
                ctx.BlockIndex,
                EquipmentIds.Count,
                cost * currency
            );
            avatarState.Update(mail);

            if (migrationRequired)
            {
                states = states
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize());
            }

            return states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .MintAsset(AvatarAddress, cost * currency);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["e"] = new List(EquipmentIds.OrderBy(i => i).Select(i => i.Serialize())),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
        }
    }
}
