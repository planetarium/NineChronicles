using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("grinding")]
    public class Grinding : GameAction
    {
        public const int CostAp = 5;
        public const int Limit = 10;
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

            if (!EquipmentIds.Any() || EquipmentIds.Count > Limit)
            {
                throw new InvalidItemCountException();
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

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(CrystalEquipmentGrindingSheet),
                typeof(CrystalMonsterCollectionMultiplierSheet)
            });

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

            List<Equipment> equipmentList = new List<Equipment>();
            foreach (var equipmentId in EquipmentIds)
            {
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
                equipmentList.Add(equipment);
            }

            FungibleAssetValue crystal = CalculateCrystal(equipmentList,
                sheets.GetSheet<CrystalEquipmentGrindingSheet>(), monsterCollectionLevel,
                sheets.GetSheet<CrystalMonsterCollectionMultiplierSheet>());

            var mail = new GrindingMail(
                ctx.BlockIndex,
                Id,
                ctx.BlockIndex,
                EquipmentIds.Count,
                crystal
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
                .MintAsset(AvatarAddress, crystal);
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
        public static FungibleAssetValue CalculateCrystal(
            IEnumerable<Equipment> equipmentList,
            CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
            int monsterCollectionLevel,
            CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet
        )
        {
            Currency currency = new Currency("CRYSTAL", 18, minters: null);
            FungibleAssetValue crystal = 0 * currency;
            foreach (var equipment in equipmentList)
            {
                CrystalEquipmentGrindingSheet.Row grindingRow = crystalEquipmentGrindingSheet[equipment.Id];
                int level = Math.Max(0, equipment.level - 1);
                crystal += BigInteger.Pow(2, level) * grindingRow.CRYSTAL * currency;
            }
            CrystalMonsterCollectionMultiplierSheet.Row multiplierRow =
                crystalMonsterCollectionMultiplierSheet[monsterCollectionLevel];
            var extra = crystal.DivRem(10, out _) * (multiplierRow.Multiplier / 10);
            return crystal + extra;
        }
    }
}
