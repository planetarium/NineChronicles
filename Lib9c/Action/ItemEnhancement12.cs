using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Updated at https://github.com/planetarium/lib9c/pull/2068
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200071ObsoleteIndex)]
    [ActionType("item_enhancement12")]
    public class ItemEnhancement12 : GameAction, IItemEnhancementV4
    {
        public enum EnhancementResult
        {
            // Result is fixed to Success.
            // GreatSuccess = 0,
            Success = 1,
            // Fail = 2,
        }

        public const int MaterialCountLimit = 50;

        public Guid itemId;
        public List<Guid> materialIds;
        public Address avatarAddress;
        public int slotIndex;

        Guid IItemEnhancementV4.ItemId => itemId;
        List<Guid> IItemEnhancementV4.MaterialIds => materialIds;
        Address IItemEnhancementV4.AvatarAddress => avatarAddress;
        int IItemEnhancementV4.SlotIndex => slotIndex;

        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            protected override string TypeId => "item_enhancement12.result";
            public Guid id;
            public IEnumerable<Guid> materialItemIdList;
            public BigInteger gold;
            public int actionPoint;
            public EnhancementResult enhancementResult;
            public ItemUsable preItemUsable;
            public FungibleAssetValue CRYSTAL;

            public ResultModel()
            {
            }

            public ResultModel(Dictionary serialized) : base(serialized)
            {
                id = serialized["id"].ToGuid();
                materialItemIdList =
                    serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
                gold = serialized["gold"].ToBigInteger();
                actionPoint = serialized["actionPoint"].ToInteger();
                enhancementResult = serialized["enhancementResult"].ToEnum<EnhancementResult>();
                preItemUsable = serialized.ContainsKey("preItemUsable")
                    ? (ItemUsable)ItemFactory.Deserialize((Dictionary)serialized["preItemUsable"])
                    : null;
                CRYSTAL = serialized["c"].ToFungibleAssetValue();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text)"id"] = id.Serialize(),
                    [(Text)"materialItemIdList"] = materialItemIdList
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize(),
                    [(Text)"gold"] = gold.Serialize(),
                    [(Text)"actionPoint"] = actionPoint.Serialize(),
                    [(Text)"enhancementResult"] = enhancementResult.Serialize(),
                    [(Text)"preItemUsable"] = preItemUsable.Serialize(),
                    [(Text)"c"] = CRYSTAL.Serialize(),
                }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["itemId"] = itemId.Serialize(),
                    ["materialIds"] = new List(
                        materialIds.OrderBy(i => i).Select(i => i.Serialize())
                    ),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                    ["slotIndex"] = slotIndex.Serialize(),
                };

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialIds = plainValue["materialIds"].ToList(StateExtensions.ToGuid);
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text)"slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);
            var ctx = context;
            var states = ctx.PreviousState;

            // Collect addresses
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
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex} ItemEnhancement exec started", addressesHex);

            // Validate avatar
            if (!states.TryGetAgentAvatarStatesV2(ctx.Signer, avatarAddress, out var agentState,
                    out var avatarState, out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load."
                );
            }

            // Validate AP
            var requiredActionPoint = GetRequiredAp();
            if (avatarState.actionPoint < requiredActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex} Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredActionPoint}"
                );
            }

            // Validate target equipment item
            if (!avatarState.inventory.TryGetNonFungibleItem(itemId,
                    out ItemUsable enhancementItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex} Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex} Aborted as the equipment to enhance ({itemId}) is not available yet;" +
                    $" it will be available at the block #{enhancementItem.RequiredBlockIndex}."
                );
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                throw new InvalidCastException(
                    $"{addressesHex} Aborted as the item is not a {nameof(Equipment)}, but {enhancementItem.GetType().Name}."
                );
            }

            // Validate combination slot
            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the slot state was failed to load. #{slotIndex}"
                );
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex} Aborted as the slot state was failed to invalid. #{slotIndex}"
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} ItemEnhancement Get Equipment: {Elapsed}", addressesHex,
                sw.Elapsed);

            sw.Restart();

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemSheet),
                typeof(EnhancementCostSheetV3),
                typeof(MaterialItemSheet),
                typeof(CrystalEquipmentGrindingSheet),
                typeof(CrystalMonsterCollectionMultiplierSheet),
                typeof(StakeRegularRewardSheet)
            });

            // Validate from sheet
            var enhancementCostSheet = sheets.GetSheet<EnhancementCostSheetV3>();
            EnhancementCostSheetV3.Row startCostRow;
            if (enhancementEquipment.level == 0)
            {
                startCostRow = new EnhancementCostSheetV3.Row();
            }
            else
            {
                if (!TryGetRow(enhancementEquipment, enhancementCostSheet, out startCostRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet),
                        enhancementEquipment.level);
                }
            }

            var maxLevel = GetEquipmentMaxLevel(enhancementEquipment, enhancementCostSheet);
            if (enhancementEquipment.level > maxLevel)
            {
                throw new EquipmentLevelExceededException(
                    $"{addressesHex} Aborted due to invalid equipment level: {enhancementEquipment.level} < {maxLevel}");
            }

            // Validate enhancement materials
            if (!materialIds.Any() || materialIds.Count > MaterialCountLimit)
            {
                throw new InvalidItemCountException();
            }

            var materialEquipments = new List<Equipment>();

            foreach (var materialId in materialIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId,
                        out ItemUsable materialItem))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex} Aborted as the signer does not have a necessary material ({materialId})."
                    );
                }

                if (materialItem.RequiredBlockIndex > context.BlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"{addressesHex} Aborted as the material ({materialId}) is not available yet;" +
                        $" it will be available at the block #{materialItem.RequiredBlockIndex}."
                    );
                }

                if (!(materialItem is Equipment materialEquipment))
                {
                    throw new InvalidCastException(
                        $"{addressesHex} Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                    );
                }

                if (enhancementEquipment.ItemId == materialId)
                {
                    throw new InvalidMaterialException(
                        $"{addressesHex} Aborted as an equipment to enhance ({materialId}) was used as a material too."
                    );
                }

                if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
                {
                    throw new InvalidMaterialException(
                        $"{addressesHex} Aborted as the material item is not a {enhancementEquipment.ItemSubType}," +
                        $" but {materialEquipment.ItemSubType}."
                    );
                }

                materialEquipments.Add(materialEquipment);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} ItemEnhancement Get Material: {Elapsed}",
                addressesHex, sw.Elapsed);

            sw.Restart();

            // Do the action
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            // Subtract required action point
            avatarState.actionPoint -= requiredActionPoint;

            // Unequip items
            enhancementEquipment.Unequip();
            foreach (var materialEquipment in materialEquipments)
            {
                materialEquipment.Unequip();
            }

            // clone enhancement item
            var preItemUsable = new Equipment((Dictionary)enhancementEquipment.Serialize());

            // Equipment level up & Update
            enhancementEquipment.Exp = enhancementEquipment.GetRealExp(equipmentItemSheet,
                enhancementCostSheet);

            enhancementEquipment.Exp +=
                materialEquipments.Aggregate(0L,
                    (total, m) => total + m.GetRealExp(equipmentItemSheet, enhancementCostSheet));
            var row = enhancementCostSheet
                .OrderByDescending(r => r.Value.Exp)
                .FirstOrDefault(row =>
                    row.Value.ItemSubType == enhancementEquipment.ItemSubType &&
                    row.Value.Grade == enhancementEquipment.Grade &&
                    row.Value.Exp <= enhancementEquipment.Exp
                ).Value;

            var random = ctx.GetRandom();
            if (!(row is null) && row.Level > enhancementEquipment.level)
            {
                enhancementEquipment.SetLevel(random, row.Level, enhancementCostSheet);
            }

            EnhancementCostSheetV3.Row targetCostRow;
            if (enhancementEquipment.level == 0)
            {
                targetCostRow = new EnhancementCostSheetV3.Row();
            }
            else
            {
                if (!TryGetRow(enhancementEquipment, enhancementCostSheet, out targetCostRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet),
                        enhancementEquipment.level);
                }
            }

            // TransferAsset (NCG)
            // Total cost = Total cost to reach target level - total cost to reach start level (already used)
            var requiredNcg = targetCostRow.Cost - startCostRow.Cost;
            if (requiredNcg > 0)
            {
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress =
                    Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                states = states.TransferAsset(ctx, ctx.Signer, feeStoreAddress,
                    states.GetGoldCurrency() * requiredNcg);
            }

            // Required block index = Total required block to reach target level - total required block to reach start level (already elapsed)
            var requiredBlockIndex =
                ctx.BlockIndex +
                (targetCostRow.RequiredBlockIndex - startCostRow.RequiredBlockIndex);
            enhancementEquipment.Update(requiredBlockIndex);

            // Remove materials
            foreach (var materialId in materialIds)
            {
                avatarState.inventory.RemoveNonFungibleItem(materialId);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} ItemEnhancement Upgrade Equipment: {Elapsed}", addressesHex,
                sw.Elapsed);

            // Send scheduled mail
            var result = new ResultModel
            {
                preItemUsable = preItemUsable,
                itemUsable = enhancementEquipment,
                materialItemIdList = materialIds.ToArray(),
                actionPoint = requiredActionPoint,
                enhancementResult = EnhancementResult.Success, // Result is fixed to Success
                gold = requiredNcg,
                CRYSTAL = 0 * CrystalCalculator.CRYSTAL,
            };

            var mail = new ItemEnhanceMail(
                result, ctx.BlockIndex, random.GenerateRandomGuid(), requiredBlockIndex
            );
            result.id = mail.id;
            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            // Update quest reward
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
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
            Log.Verbose("{AddressesHex} ItemEnhancement Set AvatarState: {Elapsed}", addressesHex,
                sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex} ItemEnhancement Total Executed Time: {Elapsed}", addressesHex,
                ended - started);
            return states.SetState(slotAddress, slotState.Serialize());
        }

        public static int GetRequiredBlockCount(Equipment preEquipment, Equipment targetEquipment,
            EnhancementCostSheetV3 sheet)
        {
            return sheet.OrderedList
                .Where(e =>
                    e.ItemSubType == targetEquipment.ItemSubType &&
                    e.Grade == targetEquipment.Grade &&
                    e.Level > preEquipment.level &&
                    e.Level <= targetEquipment.level)
                .Aggregate(0, (blocks, row) => blocks + row.RequiredBlockIndex);
        }

        public static bool TryGetRow(Equipment equipment, EnhancementCostSheetV3 sheet,
            out EnhancementCostSheetV3.Row row)
        {
            row = sheet.OrderedList.FirstOrDefault(x =>
                x.Grade == equipment.Grade &&
                x.Level == equipment.level &&
                x.ItemSubType == equipment.ItemSubType
            );
            return row != null;
        }

        public static int GetEquipmentMaxLevel(Equipment equipment, EnhancementCostSheetV3 sheet)
        {
            return sheet.OrderedList.Where(x => x.Grade == equipment.Grade).Max(x => x.Level);
        }

        public static int GetRequiredAp()
        {
            return GameConfig.EnhanceEquipmentCostAP;
        }
    }
}
