using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public BigInteger Gold;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "gold"] = Gold.Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            Gold = dict["gold"].ToBigInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Miner, MarkChanged);
            }

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

            // FIXME: 사실 여기서 mint를 바로 하면 안되고 미리 펀드 같은 걸 만들어서 거기로부터 TransferAsset()해야 함...
            return states.MintAsset(ctx.Miner, Currencies.Gold, Gold);
        }
    }
}
