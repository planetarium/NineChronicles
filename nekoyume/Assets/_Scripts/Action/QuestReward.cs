using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("quest_reward")]
    public class QuestReward : GameAction
    {
        public int questId;
        public Address avatarAddress;
        public Game.Quest.Quest Result;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out var agentState, out var avatarState))
            {
                return states;
            }

            var quest = avatarState.questList.FirstOrDefault(i => i.Id == questId && i.Complete && !i.Receive);
            if (quest is null)
            {
                return states;
            }

            avatarState.UpdateFromQuestReward(quest, ctx.Random, ctx);

            quest.Receive = true;

            Result = quest;

            return states
                .SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["questId"] = questId.Serialize(),
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            questId = plainValue["questId"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

    }
}
