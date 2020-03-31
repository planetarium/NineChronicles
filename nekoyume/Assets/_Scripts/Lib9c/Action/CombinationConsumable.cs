using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
            public decimal gold;
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
                gold = serialized["gold"].ToDecimal();
                actionPoint = serialized["actionPoint"].ToInteger();
                recipeId = serialized["recipeId"].ToInteger();
                subRecipeId = serialized["subRecipeId"].ToNullableInteger();
                itemType = itemUsable.Data.ItemType;
            }

            public override IValue Serialize() =>
                new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "materials"] = materials.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                    [(Text) "recipeId"] = recipeId.Serialize(),
                    [(Text) "subRecipeId"] = subRecipeId.Serialize(),
                }.Union((Dictionary) base.Serialize()));
        }

        public Dictionary<Material, int> Materials { get; private set; }
        public Address AvatarAddress;
        public int slotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["Materials"] = Materials.Serialize(),
                ["avatarAddress"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();

        public CombinationConsumable()
        {
            Materials = new Dictionary<Material, int>();
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Materials = plainValue["Materials"].ToDictionary_Material_int();
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
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
                return states;
            }

            sw.Stop();
            Log.Debug($"Combination Get AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                out var world))
                return states;

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.CombinationEquipmentAction)
            {
                // 스테이지 클리어 부족 에러.
                return states;
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, slotIndex);
            if (slotState is null || !(slotState.Validate(avatarState, ctx.BlockIndex)))
            {
                return states;
            }

            var tableSheets = TableSheets.FromActionContext(ctx);
            sw.Stop();
            Log.Debug($"Combination Get TableSheetsState: {sw.Elapsed}");
            sw.Restart();

            Log.Debug($"Execute Combination. player : `{AvatarAddress}`");

            // 사용한 재료를 인벤토리에서 제거.
            foreach (var pair in Materials)
            {
                if (!avatarState.inventory.RemoveFungibleItem(pair.Key, pair.Value))
                {
                    // 재료 부족 에러.
                    return states;
                }
            }

            sw.Stop();
            Log.Debug($"Combination Remove Materials: {sw.Elapsed}");
            sw.Restart();

            var result = new ResultModel
            {
                materials = Materials,
                itemType = ItemType.Consumable,
            };

            var materialRows = Materials.ToDictionary(pair => pair.Key.Data, pair => pair.Value);
            var consumableItemRecipeSheet = tableSheets.ConsumableItemRecipeSheet;
            var consumableItemSheet = tableSheets.ConsumableItemSheet;
            var foodMaterials = materialRows.Keys.Where(pair => pair.ItemSubType == ItemSubType.FoodMaterial);
            var foodCount = materialRows.Min(pair => pair.Value);
            var costAP = foodCount * GameConfig.CombineConsumableCostAP;
            sw.Stop();
            Log.Debug($"Combination Get Food Material rows: {sw.Elapsed}");
            sw.Restart();

            if (avatarState.actionPoint < costAP)
            {
                // ap 부족 에러.
                return states;
            }

            // ap 차감.
            avatarState.actionPoint -= costAP;
            result.actionPoint = costAP;

            // 재료가 레시피에 맞지 않다면 200000(맛 없는 요리).
            var resultConsumableItemId = !consumableItemRecipeSheet.TryGetValue(foodMaterials, out var recipeRow)
                ? GameConfig.CombinationDefaultFoodId
                : recipeRow.ResultConsumableItemId;
            sw.Stop();
            Log.Debug($"Combination Get Food id: {sw.Elapsed}");
            sw.Restart();
            // FIXME 장비조합처럼 레시피 아이디만 받아다 만드는 방식으로 변경해야함. 현재 방식이면 맛없는 요리인걸 결과슬롯에서 미리 알 수 있다.
            result.recipeId = recipeRow.Id;

            if (!consumableItemSheet.TryGetValue(resultConsumableItemId, out var consumableItemRow))
            {
                // 소모품 테이블 값 가져오기 실패.
                return states;
            }

            // 조합 결과 획득.
            for (var i = 0; i < foodCount; i++)
            {
                var itemId = ctx.Random.GenerateRandomGuid();
                var itemUsable = GetFood(consumableItemRow, itemId, ctx.BlockIndex);
                // 액션 결과
                result.itemUsable = itemUsable;
                var mail = new CombinationMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
                result.id = mail.id;
                avatarState.Update(mail);
                avatarState.UpdateFromCombination(itemUsable);
                sw.Stop();
                Log.Debug($"Combination Update AvatarState: {sw.Elapsed}");
                sw.Restart();
            }

            avatarState.UpdateQuestRewards(ctx);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.blockIndex = ctx.BlockIndex;
            states = states.SetState(AvatarAddress, avatarState.Serialize());
            slotState.Update(result, ctx.BlockIndex, ctx.BlockIndex);
            sw.Stop();
            Log.Debug($"Combination Set AvatarState: {sw.Elapsed}");
            var ended = DateTimeOffset.UtcNow;
            Log.Debug($"Combination Total Executed Time: {ended - started}");
            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(slotAddress, slotState.Serialize());
        }

        private static ItemUsable GetFood(ConsumableItemSheet.Row equipmentItemRow, Guid itemId, long ctxBlockIndex)
        {
            // FixMe. 소모품에 랜덤 스킬을 할당했을 때, `HackAndSlash` 액션에서 예외 발생. 그래서 소모품은 랜덤 스킬을 할당하지 않음.
            /*
             * InvalidTxSignatureException: 8383de6800f00416bfec1be66745895134083b431bd48766f1f6c50b699f6708: The signature (3045022100c2fffb0e28150fd6ddb53116cc790f15ca595b19ba82af8c6842344bd9f6aae10220705c37401ff35c3eb471f01f384ea6a110dd7e192d436ca99b91c9bed9b6db17) is failed to verify.
             * Libplanet.Tx.Transaction`1[T].Validate () (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blocks.Block`1[T].Validate (System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Store.BlockSet`1[T].set_Item (Libplanet.HashDigest`1[T] key, Libplanet.Blocks.Block`1[T] value) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].Append (Libplanet.Blocks.Block`1[T] block, System.DateTimeOffset currentTime, System.Boolean render) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].Append (Libplanet.Blocks.Block`1[T] block, System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].MineBlock (Libplanet.Address miner, System.DateTimeOffset currentTime) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Libplanet.Blockchain.BlockChain`1[T].MineBlock (Libplanet.Address miner) (at <7284bf7c1f1547329a0963c7fa3ab23e>:0)
             * Nekoyume.BlockChain.Agent+<>c__DisplayClass31_0.<CoMiner>b__0 () (at Assets/_Scripts/BlockChain/Agent.cs:168)
             * System.Threading.Tasks.Task`1[TResult].InnerInvoke () (at <1f0c1ef1ad524c38bbc5536809c46b48>:0)
             * System.Threading.Tasks.Task.Execute () (at <1f0c1ef1ad524c38bbc5536809c46b48>:0)
             * UnityEngine.Debug:LogException(Exception)
             * Nekoyume.BlockChain.<CoMiner>d__31:MoveNext() (at Assets/_Scripts/BlockChain/Agent.cs:208)
             * UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)
             */
            return ItemFactory.CreateItemUsable(equipmentItemRow, itemId, ctxBlockIndex);
        }
    }
}
