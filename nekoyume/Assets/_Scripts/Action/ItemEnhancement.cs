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
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("item_enhancement")]
    public class ItemEnhancement : GameAction
    {
        public const decimal RequiredGoldPerLevel = 5m;
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

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable item))
            {
                return states;
            }

            var materials = new List<Equipment>();
            var options = new List<object>();
            var materialOptionCount = 0;
            foreach (var materialId in materialIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable nonFungibleItem))
                {
                    return states;
                }

                if (materials.Contains(nonFungibleItem))
                {
                    Debug.LogWarning($"Duplicate materials found. {nonFungibleItem}");
                    return states;
                }

                if (item.ItemId == materialId)
                {
                    return states;
                }

                if (nonFungibleItem.Data.ItemSubType != item.Data.ItemSubType)
                {
                    Debug.LogWarning($"Expected ItemSubType is {item.Data.ItemSubType}. " +
                                     "but Material SubType is {material.Data.ItemSubType}");
                    return states;
                }

                var material = (Equipment) nonFungibleItem;
                material.Unequip();
                materials.Add(material);
                var materialOptions = material.GetOptions();
                options.AddRange(materialOptions);
                materialOptionCount = Math.Max(materialOptionCount, materialOptions.Count);
            }

            var equipment = (Equipment) item;
            equipment.Unequip();
            var equipmentOptions = equipment.GetOptions();
            options.AddRange(equipmentOptions);
            var equipmentOptionCount = Math.Max(materialOptionCount, equipmentOptions.Count);
            equipment = UpgradeEquipment(equipment, ctx.Random, options, equipmentOptionCount);
            var requiredGold = Math.Max(RequiredGoldPerLevel, RequiredGoldPerLevel * equipment.level * equipment.level);

            if (agentState.gold < requiredGold)
            {
                return states;
            }

            agentState.gold -= requiredGold;
            foreach (var material in materials)
            {
                avatarState.inventory.RemoveNonFungibleItem(material);
            }

            result = new Result {itemUsable = equipment};
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex);
            avatarState.inventory.RemoveNonFungibleItem(equipment);
            avatarState.Update(mail);
            avatarState.UpdateItemEnhancementQuest(equipment);
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

        private static BuffSkill GetRandomBuffSkill(IRandom random)
        {
            var skillRows = Game.Game.instance.TableSheets.SkillSheet.OrderedList
                .Where(i => (i.SkillType == SkillType.Debuff || i.SkillType == SkillType.Buff) &&
                            i.SkillCategory != SkillCategory.Heal)
                .ToList();
            var skillRow = skillRows[random.Next(0, skillRows.Count)];
            return (BuffSkill) SkillFactory.Get(skillRow, 0, 100);
        }

        private static Equipment UpgradeEquipment(Equipment equipment, IRandom random, IEnumerable<object> options, int count)
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
                    case StatsMap statsMap:
                    {
                        if (statsMap.HasAdditionalHP)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.HP, statsMap.AdditionalHP);
                        }
                        if (statsMap.HasAdditionalATK)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.ATK, statsMap.AdditionalATK);
                        }
                        if (statsMap.HasAdditionalCRI)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.CRI, statsMap.AdditionalCRI);
                        }
                        if (statsMap.HasAdditionalDEF)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.DEF, statsMap.AdditionalDEF);
                        }
                        if (statsMap.HasAdditionalDOG)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.DOG, statsMap.AdditionalDOG);
                        }
                        if (statsMap.HasAdditionalSPD)
                        {
                            equipment.StatsMap.SetStatAdditionalValue(StatType.SPD, statsMap.AdditionalSPD);
                        }
                        break;
                    }
                }
                sortedSkills.Remove(selected);
            }

            equipment.LevelUp();

            return equipment;
        }
    }
}
