using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
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
    [ActionType("item_enhancement")]
    public class ItemEnhancement : GameAction
    {
        public const int RequiredBlockCount = 1;

        public static readonly Address BlacksmithAddress = Addresses.Blacksmith;

        public Guid itemId;
        public IEnumerable<Guid> materialIds;
        public Address avatarAddress;
        public int slotIndex;

        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            protected override string TypeId => "itemEnhancement.result";
            public Guid id;
            public IEnumerable<Guid> materialItemIdList;
            public BigInteger gold;
            public int actionPoint;

            public ResultModel()
            {
            }

            public ResultModel(Bencodex.Types.Dictionary serialized)
                : base(serialized)
            {
                id = serialized["id"].ToGuid();
                materialItemIdList = serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
                gold = serialized["gold"].ToBigInteger();
                actionPoint = serialized["actionPoint"].ToInteger();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "materialItemIdList"] = materialItemIdList
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

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
            Log.Warning($"{nameof(ItemEnhancement)} is deprecated. Please use ${nameof(ItemEnhancement2)}");

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("ItemEnhancement exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("ItemEnhancement Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"Aborted as the equipment to enhance ({itemId}) is not available yet; it will be available at the block #{enhancementItem.RequiredBlockIndex}."
                );
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                throw new InvalidCastException(
                    $"Aborted as the item is not a {nameof(Equipment)}, but {enhancementItem.GetType().Name}."

                );
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"Aborted as the slot state was failed to load. #{slotIndex}");
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException($"Aborted as the slot state was failed to invalid. #{slotIndex}");
            }

            sw.Stop();
            Log.Debug("ItemEnhancement Get Equipment: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if(enhancementEquipment.level > 9)
            {
                // Maximum level exceeded.
                throw new EquipmentLevelExceededException(
                    $"Aborted due to invalid equipment level: {enhancementEquipment.level} < 9"
                );
            }

            var result = new ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = materialIds
            };

            var requiredAP = GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                throw new NotEnoughActionPointException(
                    $"Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredAP}"
                );
            }

            var enhancementCostSheet = states.GetSheet<EnhancementCostSheet>();
            var requiredNCG = GetRequiredNCG(enhancementCostSheet, enhancementEquipment.Grade, enhancementEquipment.level + 1);

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
                        $"Aborted as the signer does not have a necessary material ({materialId})."
                    );
                }

                if (materialItem.RequiredBlockIndex > context.BlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"Aborted as the material ({materialId}) is not available yet; it will be available at the block #{materialItem.RequiredBlockIndex}."
                    );
                }

                if (!(materialItem is Equipment materialEquipment))
                {
                    throw new InvalidCastException(
                        $"Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                    );
                }

                if (materials.Contains(materialEquipment))
                {
                    throw new DuplicateMaterialException(
                        $"Aborted as the same material was used more than once: {materialEquipment}"
                    );
                }

                if (enhancementEquipment.ItemId == materialId)
                {
                    throw new InvalidMaterialException(
                        $"Aborted as an equipment to enhance ({materialId}) was used as a material too."
                    );
                }

                if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
                {
                    // Invalid ItemSubType
                    throw new InvalidMaterialException(
                        $"Aborted as the material item is not a {enhancementEquipment.ItemSubType}, but {materialEquipment.ItemSubType}."
                    );
                }

                if (materialEquipment.Grade != enhancementEquipment.Grade)
                {
                    // Invalid Grade
                    throw new InvalidMaterialException(
                        $"Aborted as grades of the equipment to enhance ({enhancementEquipment.Grade}) and a material ({materialEquipment.Grade}) does not match."
                    );
                }

                if (materialEquipment.level != enhancementEquipment.level)
                {
                    // Invalid level
                    throw new InvalidMaterialException(
                        $"Aborted as levels of the equipment to enhance ({enhancementEquipment.level}) and a material ({materialEquipment.level}) does not match."
                    );
                }
                sw.Stop();
                Log.Debug("ItemEnhancement Get Material: {Elapsed}", sw.Elapsed);
                sw.Restart();
                materialEquipment.Unequip();
                materials.Add(materialEquipment);
            }

            enhancementEquipment.Unequip();

            enhancementEquipment = UpgradeEquipment(enhancementEquipment);

            var requiredBlockIndex = ctx.BlockIndex + RequiredBlockCount;
            enhancementEquipment.Update(requiredBlockIndex);
            sw.Stop();
            Log.Debug("ItemEnhancement Upgrade Equipment: {Elapsed}", sw.Elapsed);
            sw.Restart();

            result.gold = 0;

            foreach (var material in materials)
            {
                avatarState.inventory.RemoveNonFungibleItem(material);
            }
            sw.Stop();
            Log.Debug("ItemEnhancement Remove Materials: {Elapsed}", sw.Elapsed);
            sw.Restart();
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), requiredBlockIndex);
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);

            sw.Stop();
            Log.Debug("ItemEnhancement Update AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();
            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("ItemEnhancement Set AvatarState: {Elapsed}", sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("ItemEnhancement Total Executed Time: {Elapsed}", ended - started);
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

        public static BigInteger GetRequiredNCG(EnhancementCostSheet costSheet, int grade, int level)
        {
            var row = costSheet
                .OrderedList
                .FirstOrDefault(x => x.Grade == grade && x.Level == level);

            return row?.Cost ?? 0;
        }

        public static Equipment UpgradeEquipment(Equipment equipment)
        {
            equipment.LevelUp();
            return equipment;
        }

        public static int GetRequiredAp()
        {
            return GameConfig.EnhanceEquipmentCostAP;
        }
    }
}
