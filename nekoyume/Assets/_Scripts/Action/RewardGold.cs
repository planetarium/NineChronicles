using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public decimal gold;
        
        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["gold"] = gold.ToString(CultureInfo.InvariantCulture),
            }.ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            gold = decimal.Parse(plainValue["gold"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
                return states.SetState(ctx.Miner, MarkChanged);
            }

            // 랭킹보드, 상점을 블록체인에 한번만 새로 생성하게 하기 위함.
            // 다른 액션에서는 항상 블록체인에 상태들이 존재한다고 가정합니다.
            if (ctx.BlockIndex == 0)
            {
                states = states.SetState(RankingState.Address, new RankingState());
                states = states.SetState(ShopState.Address, new ShopState());
            }

            var agentState = (AgentState) states.GetState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += gold;

            return states.SetState(ctx.Miner, agentState);
        }
    }
}
