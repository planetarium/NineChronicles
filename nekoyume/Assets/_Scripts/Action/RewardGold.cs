using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public decimal gold;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "gold"] = gold.Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            gold = dict["gold"].ToDecimal();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(DailyBlockState.Address, MarkChanged);
                return states.SetState(ctx.Miner, MarkChanged);
            }

            // 랭킹보드, 상점을 블록체인에 한번만 새로 생성하게 하기 위함.
            // 다른 액션에서는 항상 블록체인에 상태들이 존재한다고 가정합니다.
            if (ctx.BlockIndex == 0)
            {
                states = states
                    .SetState(RankingState.Address, new RankingState().Serialize())
                    .SetState(ShopState.Address, new ShopState().Serialize())
                    .SetState(DailyBlockState.Address, new DailyBlockState(0).Serialize());
            }
            else
            {
                if (ctx.BlockIndex % DailyBlockState.UpdateInterval == 0)
                {
                    states = states.SetState(
                        DailyBlockState.Address,
                        new DailyBlockState(ctx.BlockIndex).Serialize()
                    );
                }
            }

            AgentState agentState = states.GetAgentState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += gold;

            return states.SetState(ctx.Miner, agentState.Serialize());
        }
    }
}
