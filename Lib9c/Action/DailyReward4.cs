using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("daily_reward4")]
    public class DailyReward4 : GameAction, IDailyRewardV1
    {
        public Address avatarAddress;
        public DailyReward2.DailyRewardResult dailyRewardResult;
        private const int rewardItemId = 400000;
        private const int rewardItemCount = 10;

        Address IDailyRewardV1.AvatarAddress => avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(avatarAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(ctx.Signer, avatarAddress, out _, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= gameConfigState.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = gameConfigState.ActionPointMax;
            }

            // create item
            var materialSheet = states.GetSheet<MaterialItemSheet>();
            var materials = new Dictionary<Material, int>();
            var material = ItemFactory.CreateMaterial(materialSheet, rewardItemId);
            materials[material] = rewardItemCount;

            var result = new DailyReward2.DailyRewardResult
            {
                materials = materials,
            };

            // create mail
            var mail = new DailyRewardMail(result,
                                           ctx.BlockIndex,
                                           ctx.Random.GenerateRandomGuid(),
                                           ctx.BlockIndex);

            result.id = mail.id;
            dailyRewardResult = result;
            avatarState.Update(mail);
            avatarState.UpdateFromAddItem2(material, rewardItemCount, false);
            return states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
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
