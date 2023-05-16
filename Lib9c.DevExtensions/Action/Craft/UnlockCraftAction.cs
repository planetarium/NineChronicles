using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Lib9c.DevExtensions.Action.Craft
{
    [Serializable]
    [ActionType("unlock_craft_action")]
    public class UnlockCraftAction : GameAction
    {
        public Address AvatarAddress { get; set; }
        public ActionTypeAttribute ActionType { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            int targetStage;

            if (ActionType.TypeIdentifier is Text text)
            {
                if (text.Value.Contains("combination_equipment"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                }
                else if (text.Value.Contains("combination_consumable"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
                }
                else if (text.Value.Contains("item_enhancement"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.ItemEnhancementAction;
                }
                else
                {
                    throw new InvalidActionFieldException(
                        $"{ActionType.TypeIdentifier} is not valid action");
                }
            }
            else
            {
                throw new InvalidActionFieldException(
                    $"{ActionType.TypeIdentifier} is not valid action");
            }

            var worldInformation = new WorldInformation(
                context.BlockIndex,
                states.GetSheet<WorldSheet>(),
                targetStage
            );
            return states.SetState(
                AvatarAddress.Derive(LegacyWorldInformationKey),
                worldInformation.Serialize()
            );
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["typeIdentifier"] = ActionType.TypeIdentifier
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
            )
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            ActionType = new ActionTypeAttribute(plainValue["typeIdentifier"].ToDotnetString());
        }
    }
}
