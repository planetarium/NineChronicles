using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("daily_reward5")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;
        public const string AvatarAddressKey = "a";

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out _, out AvatarState avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            if (avatarState.dailyRewardReceivedIndex < context.BlockIndex - gameConfigState.DailyRewardInterval)
            {
                var sb = new StringBuilder()
                    .Append($"{addressesHex}Not enough block index since the last received daily reward.")
                    .Append(
                        $" Expected: Equals or greater than {context.BlockIndex - gameConfigState.DailyRewardInterval}.")
                    .Append($" Actual: {avatarState.dailyRewardReceivedIndex}");
                throw new RequiredBlockIndexException(sb.ToString());
            }

            avatarState.dailyRewardReceivedIndex = context.BlockIndex;
            avatarState.actionPoint = gameConfigState.ActionPointMax;

            return states.SetState(avatarAddress, avatarState.SerializeV2());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [AvatarAddressKey] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
