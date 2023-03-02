using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100220ObsoleteIndex)]
    [ActionType("item_enhancement9")]
    public class ItemEnhancement9 : GameAction, IItemEnhancementV2
    {
        public static readonly Address BlacksmithAddress = Addresses.Blacksmith;

        public enum EnhancementResult
        {
            GreatSuccess = 0,
            Success = 1,
            Fail = 2,
        }

        public Guid itemId;
        public Guid materialId;
        public Address avatarAddress;
        public int slotIndex;

        Guid IItemEnhancementV2.ItemId => itemId;
        Guid IItemEnhancementV2.MaterialId => materialId;
        Address IItemEnhancementV2.AvatarAddress => avatarAddress;
        int IItemEnhancementV2.SlotIndex => slotIndex;

        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            protected override string TypeId => "item_enhancement9.result";
            public Guid id;
            public IEnumerable<Guid> materialItemIdList;
            public BigInteger gold;
            public int actionPoint;
            public EnhancementResult enhancementResult;
            public ItemUsable preItemUsable;

            public ResultModel()
            {
            }

            public ResultModel(Dictionary serialized) : base(serialized)
            {
                id = serialized["id"].ToGuid();
                materialItemIdList = serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
                gold = serialized["gold"].ToBigInteger();
                actionPoint = serialized["actionPoint"].ToInteger();
                enhancementResult = serialized["enhancementResult"].ToEnum<EnhancementResult>();
                preItemUsable = serialized.ContainsKey("preItemUsable")
                    ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized["preItemUsable"])
                    : null;
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "materialItemIdList"] = materialItemIdList
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                    [(Text) "enhancementResult"] = enhancementResult.Serialize(),
                    [(Text) "preItemUsable"] = preItemUsable.Serialize(),
                }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["itemId"] = itemId.Serialize(),
                    ["materialId"] = materialId.Serialize(),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                    ["slotIndex"] = slotIndex.Serialize(),
                };

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialId = plainValue["materialId"].ToGuid();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text) "slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var ctx = context;
            var states = ctx.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, BlacksmithAddress)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100220ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement exec started", addressesHex);
            if (!states.TryGetAgentAvatarStatesV2(ctx.Signer, avatarAddress, out var agentState, out var avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the equipment to enhance ({itemId}) is not available yet;" +
                    $" it will be available at the block #{enhancementItem.RequiredBlockIndex}."
                );
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                throw new InvalidCastException(
                    $"{addressesHex}Aborted as the item is not a {nameof(Equipment)}, but {enhancementItem.GetType().Name}."

                );
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the slot state was failed to load. #{slotIndex}");
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException($"{addressesHex}Aborted as the slot state was failed to invalid. #{slotIndex}");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get Equipment: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var enhancementCostSheet = states.GetSheet<EnhancementCostSheetV2>();
            if (!TryGetRow(enhancementEquipment, enhancementCostSheet, out var row))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), enhancementEquipment.level);
            }

            var maxLevel = GetEquipmentMaxLevel(enhancementEquipment, enhancementCostSheet);
            if(enhancementEquipment.level >= maxLevel)
            {
                throw new EquipmentLevelExceededException(
                    $"{addressesHex}Aborted due to invalid equipment level: {enhancementEquipment.level} < {maxLevel}");
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the signer does not have a necessary material ({materialId})."
                );
            }

            if (materialItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the material ({materialId}) is not available yet;" +
                    $" it will be available at the block #{materialItem.RequiredBlockIndex}."
                );
            }

            if (!(materialItem is Equipment materialEquipment))
            {
                throw new InvalidCastException(
                    $"{addressesHex}Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                );
            }

            if (enhancementEquipment.ItemId == materialId)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as an equipment to enhance ({materialId}) was used as a material too."
                );
            }

            if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as the material item is not a {enhancementEquipment.ItemSubType}," +
                    $" but {materialEquipment.ItemSubType}."
                );
            }

            if (materialEquipment.Grade != enhancementEquipment.Grade)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as grades of the equipment to enhance ({enhancementEquipment.Grade})" +
                    $" and a material ({materialEquipment.Grade}) does not match."
                );
            }

            if (materialEquipment.level != enhancementEquipment.level)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as levels of the equipment to enhance ({enhancementEquipment.level})" +
                    $" and a material ({materialEquipment.level}) does not match."
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get Material: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            // Subtract required action point
            var requiredActionPoint = GetRequiredAp();
            if (avatarState.actionPoint < requiredActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredActionPoint}"
                );
            }
            avatarState.actionPoint -= requiredActionPoint;

            // TransferAsset (NCG)
            var requiredNcg = row.Cost;
            if (requiredNcg > 0)
            {
                states = states.TransferAsset(ctx.Signer, BlacksmithAddress, states.GetGoldCurrency() * requiredNcg);
            }

            // Unequip items
            materialEquipment.Unequip();
            enhancementEquipment.Unequip();

            // clone items
            var preItemUsable = new Equipment((Dictionary) enhancementEquipment.Serialize());

            // Equipment level up & Update
            var equipmentResult = GetEnhancementResult(row, ctx.Random);
            if (equipmentResult != EnhancementResult.Fail)
            {
                enhancementEquipment.LevelUpV2(ctx.Random, row, equipmentResult == EnhancementResult.GreatSuccess);
            }
            var requiredBlockCount = GetRequiredBlockCount(row, equipmentResult);
            var requiredBlockIndex = ctx.BlockIndex + requiredBlockCount;
            enhancementEquipment.Update(requiredBlockIndex);

            // Remove material
            avatarState.inventory.RemoveNonFungibleItem(materialId);
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Upgrade Equipment: {Elapsed}", addressesHex, sw.Elapsed);

            // Send scheduled mail
            var result = new ResultModel
            {
                preItemUsable = preItemUsable,
                itemUsable = enhancementEquipment,
                materialItemIdList = new[] { materialId },
                actionPoint = requiredActionPoint,
                enhancementResult = equipmentResult,
                gold = requiredNcg,
            };

            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), requiredBlockIndex);
            result.id = mail.id;
            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            // Update quest reward
            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            // Update slot state
            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);

            // Set state
            sw.Restart();
            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.SetState(slotAddress, slotState.Serialize());
        }

        public static EnhancementResult GetEnhancementResult(EnhancementCostSheetV2.Row row, IRandom random)
        {
            var rand = random.Next(1, GameConfig.MaximumProbability + 1);
            if (rand <= row.GreatSuccessRatio)
            {
                return EnhancementResult.GreatSuccess;
            }

            return rand <= row.GreatSuccessRatio + row.SuccessRatio ? EnhancementResult.Success : EnhancementResult.Fail;
        }

        public static int GetRequiredBlockCount(EnhancementCostSheetV2.Row row, EnhancementResult result)
        {
            switch (result)
            {
                case EnhancementResult.GreatSuccess:
                    return row.GreatSuccessRequiredBlockIndex;
                case EnhancementResult.Success:
                    return row.SuccessRequiredBlockIndex;
                case EnhancementResult.Fail:
                    return row.FailRequiredBlockIndex;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        public static bool TryGetRow(Equipment equipment, EnhancementCostSheetV2 sheet, out EnhancementCostSheetV2.Row row)
        {
            var grade = equipment.Grade;
            var level = equipment.level + 1;
            var itemSubType = equipment.ItemSubType;
            row = sheet.OrderedList.FirstOrDefault(x =>
                x.Grade == grade &&
                x.Level == level &&
                x.ItemSubType == itemSubType);
            return row != null;
        }

        public static int GetEquipmentMaxLevel(Equipment equipment, EnhancementCostSheetV2 sheet)
        {
            return sheet.OrderedList.Where(x => x.Grade == equipment.Grade).Max(x => x.Level);
        }

        public static int GetRequiredAp()
        {
            return GameConfig.EnhanceEquipmentCostAP;
        }
    }
}
