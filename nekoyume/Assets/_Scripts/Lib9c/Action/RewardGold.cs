using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;
#if UNITY_EDITOR || UNITY_STANDALONE
using TentuPlay.Api;
#endif

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public decimal Gold;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "gold"] = Gold.Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            Gold = dict["gold"].ToDecimal();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Miner, MarkChanged);
            }

            var agentState = states.GetAgentState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += Gold;

            var index = (int) ctx.BlockIndex / GameConfig.WeeklyArenaInterval;
            var weekly = states.GetWeeklyArenaState(WeeklyArenaState.Addresses[index]);
            if (ctx.BlockIndex % GameConfig.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = states.GetWeeklyArenaState(WeeklyArenaState.Addresses[index - 1]);
                prevWeekly.End();
                weekly.Update(prevWeekly, ctx.BlockIndex);
                states = states.SetState(prevWeekly.address, prevWeekly.Serialize());
                states = states.SetState(weekly.address, weekly.Serialize());
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= GameConfig.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weekly.address, weekly.Serialize());
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            int r = new TPStashEvent().CurrencyGet(
                player_uuid: agentState.address.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)Gold,
                currency_total_quantity: (float)agentState.gold,
                reference_entity: "bonuses",
                reference_category_slug: "reward_gold",
                reference_slug: "RewardGold");
#endif
            return states.SetState(ctx.Miner, agentState.Serialize());
        }
    }
}
