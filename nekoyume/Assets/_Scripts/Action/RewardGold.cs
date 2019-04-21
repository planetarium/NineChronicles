using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using Libplanet.Action;
using UniRx;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public static readonly Subject<int> RewardGoldMyselfSubject = new Subject<int>();
        
        public int Gold;

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Gold = int.Parse(plainValue["gold"].ToString());
        }

        private static Context CreateRewardContext()
        {
            return new Context(null);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;

            var ctx = (Context) states.GetState(actionCtx.Signer);
            if (ReferenceEquals(ctx, null))
            {
                if (actionCtx.Rehearsal)
                {
                    ctx = CreateNovice.CreateContext("dummy");
                    return states.SetState(actionCtx.Miner, ctx);
                }

                ctx = CreateRewardContext();
            }

            ctx.gold += Gold;

            if (!actionCtx.Rehearsal &&
                actionCtx.Miner.Equals(ActionManager.instance.agentAddress))
            {
                Debug.Log($"Created reward for {actionCtx.BlockIndex},  Total Gold {ctx.gold}");
                RewardGoldMyselfSubject.OnNext(ctx.gold);
            }

            return states.SetState(actionCtx.Miner, ctx);
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["gold"] = Gold.ToString(),
        }.ToImmutableDictionary();
    }
}
