using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public int gold;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            gold = int.Parse(plainValue["gold"].ToString());
        }

        private static AvatarState CreateRewardContext()
        {
            return new AvatarState(null, address: null);
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;

            var ctx = (AvatarState) states.GetState(actionCtx.Signer) ?? CreateRewardContext();

            if (actionCtx.Rehearsal)
            {
                return states.SetState(actionCtx.Miner, ctx);
            }

            ctx.gold += gold;

            return states.SetState(actionCtx.Miner, ctx);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["gold"] = gold.ToString(),
        }.ToImmutableDictionary();
    }
}
