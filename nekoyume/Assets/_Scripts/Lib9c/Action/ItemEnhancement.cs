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
        public static readonly Address BlacksmithAddress = new Address(new byte[]
        {
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x9,
        });

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
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "materialItemIdList"] =  materialItemIdList.Select(g => g.Serialize()).Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                }.Union((Bencodex.Types.Dictionary)base.Serialize()));
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
                    .MarkBalanceChanged(Currencies.Gold, ctx.Signer, BlacksmithAddress)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("ItemEnhancement exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState _,
                out AvatarState avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("ItemEnhancement Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                // 강화 장비가 없는 에러.
                return LogError(
                    context,
                    "Aborted as the NonFungibleItem ({ItemId}) was failed to load from avatar's inventory.",
                    itemId
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                return LogError(
                    context,
                    "Aborted as the equipment to enhance ({ItemId}) is not available yet; it will be available at the block #{RequiredBlockIndex}.",
                    itemId,
                    enhancementItem.RequiredBlockIndex
                );
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                // 캐스팅 버그. 예외상황.
                return LogError(
                    context,
                    $"Aborted as the item is not a {nameof(Equipment)}, but {{ItemType}}.",
                    enhancementItem.GetType().Name
                );
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null || !(slotState.Validate(avatarState, ctx.BlockIndex)))
            {
                return LogError(context, "Aborted as the slot state was failed to load or invalid.");
            }

            sw.Stop();
            Log.Debug("ItemEnhancement Get Equipment: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var result = new ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = materialIds
            };

            var requiredAP = GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                // AP 부족 에러.
                return LogError(
                    context,
                    "Aborted due to insufficient action point: {ActionPointBalance} < {ActionCost}",
                    avatarState.actionPoint,
                    requiredAP
                );
            }

            avatarState.actionPoint -= requiredAP;
            result.actionPoint = requiredAP;

            sw.Stop();
            Log.Debug("ItemEnhancement Get TableSheets: {Elapsed}", sw.Elapsed);
            sw.Restart();
            var materials = new List<Equipment>();
            foreach (var materialId in materialIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
                {
                    // 인벤토리에 재료로 등록한 장비가 없는 에러.
                    return LogError(
                        context,
                        "Aborted as the the signer does not have a necessary material ({MaterialId}).",
                        materialId
                    );
                }

                if (materialItem.RequiredBlockIndex > context.BlockIndex)
                {
                    return LogError(
                        context,
                        "Aborted as the material ({MaterialId}) is not available yet; it will be available at the block #{RequiredBlockIndex}.",
                        materialId,
                        materialItem.RequiredBlockIndex
                    );
                }

                if (!(materialItem is Equipment materialEquipment))
                {
                    return LogError(
                        context,
                        $"Aborted as the material item is not a {nameof(Equipment)}, but {{ItemType}}.",
                        materialItem.GetType().Name
                    );
                }

                if (materials.Contains(materialEquipment))
                {
                    // 같은 guid의 아이템이 중복해서 등록된 에러.
                    return LogError(
                        context,
                        "Aborted as the same material was used more than once: {Material}",
                        materialEquipment
                    );
                }

                if (enhancementEquipment.ItemId == materialId)
                {
                    // 강화 장비와 재료로 등록한 장비가 같은 에러.
                    return LogError(
                        context,
                        "Aborted as an equipment to enhance ({ItemId}) was used as a material too.",
                        materialId
                    );
                }

                if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
                {
                    // 서브 타입이 다른 에러.
                    return LogError(
                        context,
                        "Aborted as the material item is not a {ExpectedItemSubType}, but {MaterialSubType}.",
                        enhancementEquipment.ItemSubType,
                        materialEquipment.ItemSubType
                    );
                }

                if (materialEquipment.Grade != enhancementEquipment.Grade)
                {
                    // 등급이 다른 에러.
                    return LogError(
                        context,
                        "Aborted as grades of the equipment to enhance ({EquipmentGrade}) and a material ({MaterialGrade}) do not match.",
                        enhancementEquipment.Grade,
                        materialEquipment.Grade
                    );
                }

                if (materialEquipment.level != enhancementEquipment.level)
                {
                    // 강화도가 다른 에러.
                    return LogError(
                        context,
                        "Aborted as levels of the equipment to enhance ({EquipmentLevel}) and a material ({MaterialLevel}) do not match.",
                        enhancementEquipment.level,
                        materialEquipment.level
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
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            avatarState.UpdateQuestRewards(ctx);

            slotState.Update(result, ctx.BlockIndex, ctx.BlockIndex);

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
                    ["materialIds"] = materialIds.Select(g => g.Serialize()).Serialize(),
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

        private static Equipment UpgradeEquipment(Equipment equipment)
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
