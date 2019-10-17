using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
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
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var agentState = (AgentState) states.GetState(ctx.Signer);
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState == null)
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
            states = states.SetState(ctx.Signer, agentState);
            states = states.SetState(avatarAddress, avatarState);
            return states;
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["itemId"] = itemId.ToString(),
            ["materialIds"] = ByteSerializer.Serialize(materialIds),
            ["avatarAddress"] = avatarAddress.ToByteArray(),
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            itemId = new Guid((string) plainValue["itemId"]);
            materialIds = ByteSerializer.Deserialize<List<Guid>>((byte[]) plainValue["materialIds"]);
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
        }
    }
}
