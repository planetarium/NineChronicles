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
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("combination_consumable")]
    public class CombinationConsumable : GameAction
    {
        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            public Dictionary<Material, int> materials;
            public Guid id;
            public BigInteger gold;
            public int actionPoint;
            public int recipeId;
            public int? subRecipeId;
            public ItemType itemType;

            protected override string TypeId => "combination.result-model";

            public ResultModel()
            {
            }

            public ResultModel(Dictionary serialized) : base(serialized)
            {
                materials = serialized["materials"].ToDictionary_Material_int();
                id = serialized["id"].ToGuid();
                gold = serialized["gold"].ToBigInteger();
                actionPoint = serialized["actionPoint"].ToInteger();
                recipeId = serialized["recipeId"].ToInteger();
                subRecipeId = serialized["subRecipeId"].ToNullableInteger();
                itemType = itemUsable.ItemType;
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "materials"] = materials.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                    [(Text) "recipeId"] = recipeId.Serialize(),
                    [(Text) "subRecipeId"] = subRecipeId.Serialize(),
                }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public Address AvatarAddress;
        public int recipeId;
        public int slotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["recipeId"] = recipeId.Serialize(),
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                };

                // slotIndex가 포함되지 않은채 나간 버전과 호환을 위해, 0번째 슬롯을 쓰는 경우엔 보내지 않습니다.
                if (slotIndex != 0)
                {
                    dict["slotIndex"] = slotIndex.Serialize();
                }

                return dict.ToImmutableDictionary();
            }
        }

        public CombinationConsumable()
        {
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            recipeId = plainValue["recipeId"].ToInteger();
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text) "slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var slotAddress = AvatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("Combination exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Debug("Combination Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.CombinationEquipmentAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction, current);
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if(!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }

            Log.Debug("Execute Combination; player: {Player}", AvatarAddress);
            var consumableItemSheet = states.GetSheet<ConsumableItemSheet>();
            var recipeRow = states.GetSheet<ConsumableItemRecipeSheet>().Values.FirstOrDefault(r => r.Id == recipeId);
            if (recipeRow is null)
            {
                throw new SheetRowNotFoundException(nameof(ConsumableItemRecipeSheet), recipeId);
            }
            var materials = new Dictionary<Material, int>();
            foreach (var materialInfo in recipeRow.Materials.OrderBy(r => r.Id))
            {
                var materialId = materialInfo.Id;
                var count = materialInfo.Count;
                if (avatarState.inventory.HasItem(materialId, count))
                {
                    avatarState.inventory.TryGetFungibleItem(materialId, out var inventoryItem);
                    var material = (Material) inventoryItem.item;
                    materials[material] = count;
                    avatarState.inventory.RemoveFungibleItem(material, count);
                }
                else
                {
                    throw new NotEnoughMaterialException(
                        $"Aborted as the player has no enough material ({materialId} * {count})");
                }
            }

            sw.Stop();
            Log.Debug("Combination Remove Materials: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var result = new ResultModel
            {
                materials = materials,
                itemType = ItemType.Consumable,

            };

            var costAP = recipeRow.RequiredActionPoint;
            if (avatarState.actionPoint < costAP)
            {
                throw new NotEnoughActionPointException(
                    $"Aborted due to insufficient action point: {avatarState.actionPoint} < {costAP}"
                );
            }

            // ap 차감.
            avatarState.actionPoint -= costAP;
            result.actionPoint = costAP;

            var resultConsumableItemId = recipeRow.ResultConsumableItemId;
            sw.Stop();
            Log.Debug("Combination Get Food id: {Elapsed}", sw.Elapsed);
            sw.Restart();
            result.recipeId = recipeRow.Id;

            if (!consumableItemSheet.TryGetValue(resultConsumableItemId, out var consumableItemRow))
            {
                throw new SheetRowNotFoundException(nameof(ConsumableItemSheet), resultConsumableItemId);
            }

            // 조합 결과 획득.
            var requiredBlockIndex = ctx.BlockIndex + recipeRow.RequiredBlockIndex;
            var itemId = ctx.Random.GenerateRandomGuid();
            var itemUsable = GetFood(consumableItemRow, itemId, requiredBlockIndex);
            // 액션 결과
            result.itemUsable = itemUsable;
            var mail = new CombinationMail(
                result,
                ctx.BlockIndex,
                ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex
            );
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.UpdateFromCombination(itemUsable);
            sw.Stop();
            Log.Debug("Combination Update AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;
            states = states.SetState(AvatarAddress, avatarState.Serialize());
            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);
            sw.Stop();
            Log.Debug("Combination Set AvatarState: {Elapsed}", sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("Combination Total Executed Time: {Elapsed}", ended - started);
            return states
                .SetState(slotAddress, slotState.Serialize());
        }

        private static ItemUsable GetFood(ConsumableItemSheet.Row equipmentItemRow, Guid itemId, long ctxBlockIndex)
        {
            return ItemFactory.CreateItemUsable(equipmentItemRow, itemId, ctxBlockIndex);
        }
    }
}
