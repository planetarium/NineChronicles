using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Quest;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("quest_reward")]
    public class QuestReward : GameAction
    {
        public int questId;
        public Address avatarAddress;
        public Quest Result;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            UnityEngine.Debug.Log("QuestReward exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out _, out var avatarState))
            {
                return states;
            }
            sw.Stop();
            UnityEngine.Debug.Log($"QuestReward Get AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            var quest = avatarState.questList.FirstOrDefault(i => i.Id == questId && i.Complete && !i.IsPaidInAction);
            if (quest is null)
            {
                return states;
            }

            avatarState.UpdateFromQuestReward(quest, ctx);

            sw.Stop();
            UnityEngine.Debug.Log($"QuestReward Update AvatarState: {sw.Elapsed}");
            sw.Restart();

            Result = quest;
            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            UnityEngine.Debug.Log($"QuestReward Set AvatarState: {sw.Elapsed}");
            UnityEngine.Debug.Log($"QuestReward Total Executed Time: {ended - started}");
            return states;
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
