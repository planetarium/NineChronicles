using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("item_enhancement")]
    public class ItemEnhancement : GameAction
    {
        private TableSheets _tableSheets;
        public Guid itemId;
        public IEnumerable<Guid> materialIds;
        public Address avatarAddress;
        public ResultModel result;
        public List<int> completedQuestIds;

        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            protected override string TypeId => "itemEnhancement.result";
            public Guid id;
            public IEnumerable<Guid> materialItemIdList;
            public decimal gold;
            public int actionPoint;

            public ResultModel()
            {
            }

            public ResultModel(Bencodex.Types.Dictionary serialized)
                : base(serialized)
            {
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "id"] = id.Serialize()
                }.Union((Bencodex.Types.Dictionary)base.Serialize()));
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("ItemEnhancement exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }
            sw.Stop();
            Log.Debug($"ItemEnhancement Get AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                return states;
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                return states;
            }
            sw.Stop();
            Log.Debug($"ItemEnhancement Get Equipment: {sw.Elapsed}");
            sw.Restart();

            result = new ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = materialIds
            };

            var requiredAP = GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                // AP 부족 에러.
                return states;
            }

            avatarState.actionPoint -= requiredAP;
            result.actionPoint = requiredAP;

            var requiredNCG = GetRequiredGold(enhancementEquipment);
            if (agentState.gold < requiredNCG)
            {
                // NCG 부족 에러.
                return states;
            }

            _tableSheets = TableSheets.FromActionContext(ctx);
            sw.Stop();
            Log.Debug($"ItemEnhancement Get TableSheets: {sw.Elapsed}");
            sw.Restart();
            var materials = new List<Equipment>();
            foreach (var materialId in materialIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
                {
                    // 인벤토리에 재료로 등록한 장비가 없는 에러.
                    return states;
                }

                if (!(materialItem is Equipment materialEquipment))
                {
                    return states;
                }

                if (materials.Contains(materialEquipment))
                {
                    // 같은 guid의 아이템이 중복해서 등록된 에러.
                    Log.Warning($"Duplicate materials found. {materialEquipment}");
                    return states;
                }

                if (enhancementEquipment.ItemId == materialId)
                {
                    // 강화 장비와 재료로 등록한 장비가 같은 에러.
                    return states;
                }

                if (materialEquipment.Data.ItemSubType != enhancementEquipment.Data.ItemSubType)
                {
                    // 서브 타입이 다른 에러.
                    Log.Warning($"Expected ItemSubType is {enhancementEquipment.Data.ItemSubType}. " +
                                "but Material SubType is {material.Data.ItemSubType}");
                    return states;
                }

                if (materialEquipment.Data.Grade != enhancementEquipment.Data.Grade)
                {
                    // 등급이 다른 에러.
                    return states;
                }

                if (materialEquipment.level != enhancementEquipment.level)
                {
                    // 강화도가 다른 에러.
                    return states;
                }
                sw.Stop();
                Log.Debug($"ItemEnhancement Get Material: {sw.Elapsed}");
                sw.Restart();
                materialEquipment.Unequip();
                materials.Add(materialEquipment);
            }

            enhancementEquipment.Unequip();

            enhancementEquipment = UpgradeEquipment(enhancementEquipment);
            sw.Stop();
            Log.Debug($"ItemEnhancement Upgrade Equipment: {sw.Elapsed}");
            sw.Restart();

            agentState.gold -= requiredNCG;
            result.gold = requiredNCG;

            foreach (var material in materials)
            {
                avatarState.inventory.RemoveNonFungibleItem(material);
            }
            sw.Stop();
            Log.Debug($"ItemEnhancement Remove Materials: {sw.Elapsed}");
            sw.Restart();
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid())
            {
                New = false
            };
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            completedQuestIds = avatarState.UpdateQuestRewards(ctx);

            sw.Stop();
            Log.Debug($"ItemEnhancement Update AvatarState: {sw.Elapsed}");
            sw.Restart();
            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug($"ItemEnhancement Set AvatarState: {sw.Elapsed}");
            var ended = DateTimeOffset.UtcNow;
            Log.Debug($"ItemEnhancement Total Executed Time: {ended - started}");
            return states
                .SetState(ctx.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["itemId"] = itemId.Serialize(),
            ["materialIds"] = materialIds.Select(g => g.Serialize()).Serialize(),
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialIds = plainValue["materialIds"].ToList(StateExtensions.ToGuid);
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        private BuffSkill GetRandomBuffSkill(IRandom random)
        {
            var skillRows = _tableSheets.SkillSheet.OrderedList
                .Where(i => (i.SkillType == SkillType.Debuff || i.SkillType == SkillType.Buff) &&
                            i.SkillCategory != SkillCategory.Heal)
                .ToList();
            var skillRow = skillRows[random.Next(0, skillRows.Count)];
            return (BuffSkill) SkillFactory.Get(skillRow, 0, 100);
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

        public static decimal GetRequiredGold(Equipment enhancementEquipment)
        {
            return Math.Max(GameConfig.EnhanceEquipmentCostNCG,
                GameConfig.EnhanceEquipmentCostNCG * enhancementEquipment.Data.Grade);
        }
    }
}
