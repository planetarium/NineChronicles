using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("daily_reward")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out _, out AvatarState avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new ArgumentNullException(nameof(gameConfigState), "Aborted as the game config was failed to load.");
            }

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= GameConfig.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = gameConfigState.ActionPointMax;
            }

            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
