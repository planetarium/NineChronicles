using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("open_chest")]
    public class OpenChest : GameAction
    {
        public Address avatarAddress;
        public Dictionary<HashDigest<SHA256>, int> chestList;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = states.SetState(context.Signer, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                return states;
            }

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
            }

            var tableSheets = TableSheets.FromActionContext(context);

            foreach (var pair in chestList)
            {
                var itemId = pair.Key;
                var count = pair.Value;
                if (avatarState.inventory.TryGetMaterial(itemId, out var inventoryItem) && inventoryItem.count >= count)
                {
                    var chest = (Chest) inventoryItem.item;
                    foreach (var info in chest.Rewards)
                    {
                        switch (info.Type)
                        {
                            case RewardType.Item:
                                var itemRow =
                                    tableSheets.MaterialItemSheet.Values.FirstOrDefault(r => r.Id == info.ItemId);
                                if (itemRow is null)
                                {
                                    continue;
                                }
                                var material = ItemFactory.CreateMaterial(itemRow);
                                avatarState.inventory.AddItem(material, info.Quantity);
                                break;
                            case RewardType.Gold:
                                agentState.gold += info.Quantity;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(info.Type), info.Type, null);
                        }
                    }

                    avatarState.inventory.RemoveMaterial(itemId, count);
                }
            }

            states = states.SetState(context.Signer, agentState.Serialize());
            states = states.SetState(avatarAddress, avatarState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["chest_list"] = chestList.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            chestList = plainValue["chest_list"].Deserialize();
        }
    }
}
