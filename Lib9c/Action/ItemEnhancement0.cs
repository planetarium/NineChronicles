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

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("item_enhancement")]
    public class ItemEnhancement0 : GameAction, IItemEnhancementV1
    {
        public const int RequiredBlockCount = 1;

        public static readonly Address BlacksmithAddress = Addresses.Blacksmith;

        public Guid itemId;
        public IEnumerable<Guid> materialIds;
        public Address avatarAddress;
        public int slotIndex;

        Guid IItemEnhancementV1.ItemId => itemId;
        IEnumerable<Guid> IItemEnhancementV1.MaterialIds => materialIds;
        Address IItemEnhancementV1.AvatarAddress => avatarAddress;
        int IItemEnhancementV1.SlotIndex => slotIndex;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (ctx.Rehearsal)
            {
                return states
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, BlacksmithAddress)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            Log.Warning("{AddressesHex}item_enhancement is deprecated. Please use item_enhancement2", addressesHex);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement exec started", addressesHex);

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the equipment to enhance ({itemId}) is not available yet; it will be available at the block #{enhancementItem.RequiredBlockIndex}."
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

            if(enhancementEquipment.level > 9)
            {
                // Maximum level exceeded.
                throw new EquipmentLevelExceededException(
                    $"{addressesHex}Aborted due to invalid equipment level: {enhancementEquipment.level} < 9"
                );
            }

            var result = new ItemEnhancement7.ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = materialIds
            };

            var requiredAP = ItemEnhancement.GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredAP}"
                );
            }

            var enhancementCostSheet = states.GetSheet<EnhancementCostSheet>();
            var requiredNCG = ItemEnhancement7.GetRequiredNCG(enhancementCostSheet, enhancementEquipment.Grade, enhancementEquipment.level + 1);

            avatarState.actionPoint -= requiredAP;
            result.actionPoint = requiredAP;

            if (requiredNCG > 0)
            {
                states = states.TransferAsset(
                    ctx.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * requiredNCG
                );
            }

            var materials = new List<Equipment>();
            foreach (var materialId in materialIds.OrderBy(guid => guid))
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the signer does not have a necessary material ({materialId})."
                    );
                }

                if (materialItem.RequiredBlockIndex > context.BlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"{addressesHex}Aborted as the material ({materialId}) is not available yet; it will be available at the block #{materialItem.RequiredBlockIndex}."
                    );
                }

                if (!(materialItem is Equipment materialEquipment))
                {
                    throw new InvalidCastException(
                        $"{addressesHex}Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                    );
                }

                if (materials.Contains(materialEquipment))
                {
                    throw new DuplicateMaterialException(
                        $"{addressesHex}Aborted as the same material was used more than once: {materialEquipment}"
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
                    // Invalid ItemSubType
                    throw new InvalidMaterialException(
                        $"{addressesHex}Aborted as the material item is not a {enhancementEquipment.ItemSubType}, but {materialEquipment.ItemSubType}."
                    );
                }

                if (materialEquipment.Grade != enhancementEquipment.Grade)
                {
                    // Invalid Grade
                    throw new InvalidMaterialException(
                        $"{addressesHex}Aborted as grades of the equipment to enhance ({enhancementEquipment.Grade}) and a material ({materialEquipment.Grade}) does not match."
                    );
                }

                if (materialEquipment.level != enhancementEquipment.level)
                {
                    // Invalid level
                    throw new InvalidMaterialException(
                        $"{addressesHex}Aborted as levels of the equipment to enhance ({enhancementEquipment.level}) and a material ({materialEquipment.level}) does not match."
                    );
                }
                sw.Stop();
                Log.Verbose("{AddressesHex}ItemEnhancement Get Material: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                materialEquipment.Unequip();
                materials.Add(materialEquipment);
            }

            enhancementEquipment.Unequip();

            enhancementEquipment = ItemEnhancement7.UpgradeEquipment(enhancementEquipment);

            var requiredBlockIndex = ctx.BlockIndex + RequiredBlockCount;
            enhancementEquipment.Update(requiredBlockIndex);
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Upgrade Equipment: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            result.gold = 0;

            foreach (var material in materials)
            {
                avatarState.inventory.RemoveNonFungibleItem(material);
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Remove Materials: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), requiredBlockIndex);
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update2(mail);
            avatarState.UpdateFromItemEnhancement2(enhancementEquipment);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards2(materialSheet);

            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);

            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.SetState(slotAddress, slotState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["itemId"] = itemId.Serialize(),
                    ["materialIds"] = materialIds
                        .OrderBy(i => i)
                        .Select(g => g.Serialize())
                        .Serialize(),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                };

                // slotIndex가 포함되지 않은채 나간 버전과 호환을 위해, 0번째 슬롯을 쓰는 경우엔 보내지 않습니다.
                if (slotIndex != 0)
                {
                    dict["slotIndex"] = slotIndex.Serialize();
                }

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialIds = plainValue["materialIds"].ToList(StateExtensions.ToGuid);
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text) "slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }
    }
}
