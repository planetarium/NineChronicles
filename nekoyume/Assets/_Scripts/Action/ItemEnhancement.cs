using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("item_enhancement")]
    public class ItemEnhancement : GameAction
    {
        private TableSheets _tableSheets;
        public Guid itemId;
        public IEnumerable<Guid> materialIds;
        public Address avatarAddress;
        public Result result;

        [Serializable]
        public class Result : AttachmentActionResult
        {
            protected override string TypeId => "itemEnhancement.result";

            public Result()
            {
            }

            public Result(Bencodex.Types.Dictionary serialized)
                : base(serialized)
            {
            }
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                return states;
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                return states;
            }

            _tableSheets = TableSheets.FromActionContext(ctx);
            var materials = new List<Equipment>();
            var options = new List<object>();
            var materialOptionCount = 0;
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
                    Debug.LogWarning($"Duplicate materials found. {materialEquipment}");
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
                    Debug.LogWarning($"Expected ItemSubType is {enhancementEquipment.Data.ItemSubType}. " +
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

                materialEquipment.Unequip();
                materials.Add(materialEquipment);
                var materialOptions = materialEquipment.GetOptions();
                options.AddRange(materialOptions);
                materialOptionCount = Math.Max(materialOptionCount, materialOptions.Count);
            }

            enhancementEquipment.Unequip();
            var equipmentOptions = enhancementEquipment.GetOptions();
            options.AddRange(equipmentOptions);
            var equipmentOptionCount = Math.Max(materialOptionCount, equipmentOptions.Count);
            
            enhancementEquipment = UpgradeEquipment(enhancementEquipment, ctx.Random, options, equipmentOptionCount);
            
            var requiredGold = Math.Max(GameConfig.EnhanceEquipmentCostAP,
                GameConfig.EnhanceEquipmentCostAP * enhancementEquipment.level * enhancementEquipment.level);

            if (agentState.gold < requiredGold)
            {
                return states;
            }

            agentState.gold -= requiredGold;
            
            foreach (var material in materials)
            {
                avatarState.inventory.RemoveNonFungibleItem(material);
            }

            result = new Result {itemUsable = enhancementEquipment};
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex);
            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.Update(mail);
            avatarState.UpdateItemEnhancementQuest(enhancementEquipment);
            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
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

        private static Equipment UpgradeEquipment(Equipment equipment, IRandom random, IEnumerable<object> options,
            int count)
        {
            equipment.BuffSkills.Clear();
            equipment.Skills.Clear();
            equipment.StatsMap.ClearAdditionalStats();

            var sortedSkills = options.Select(option => new {option, guid = random.GenerateRandomGuid()})
                .OrderBy(i => i.guid).ToList();
            for (var i = 0; i < count; i++)
            {
                var selected = sortedSkills[random.Next(sortedSkills.Count)];
                switch (selected.option)
                {
                    case BuffSkill buffSkill:
                        equipment.BuffSkills.Add(buffSkill);
                        break;
                    case AttackSkill attackSkill:
                        equipment.Skills.Add(attackSkill);
                        break;
                    case StatModifier statModifier:
                        equipment.StatsMap.AddStatAdditionalValue(statModifier);
                        break;
                }

                sortedSkills.Remove(selected);
            }

            equipment.LevelUp();

            return equipment;
        }
    }
}
