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

namespace Nekoyume.Action
{
    [ActionType("item_enhancement")]
    public class ItemEnhancement : GameAction
    {
        public const decimal RequiredGoldPerLevel = 5m;
        public Guid itemId;
        public List<Guid> materialIds;
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

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out var item))
            {
                return states;
            }

            var materials = new List<ItemUsable>();
            foreach (var materialId in materialIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out var material))
                {
                    return states;
                }
                materials.Add(material);
            }

            var equipment = (Equipment) item;
            equipment.LevelUp();
            equipment.BuffSkills.Add(GetRandomBuffSkill(ctx.Random));
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
            return (BuffSkill) SkillFactory.Get(skillRow, 0, 1m);
        }
    }
}
