using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("redeem_code")]
    public class RedeemCode : GameAction
    {
        public string Code { get; internal set; }

        public Address AvatarAddress {get; internal set; }

        public RedeemCode()
        {
        }

        public RedeemCode(string code, Address avatarAddress)
        {
            Code = code;
            AvatarAddress = avatarAddress;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(AvatarAddress, MarkChanged);
                states = states.SetState(context.Signer, MarkChanged);
                states = states.MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address);
                states = states.MarkBalanceChanged(GoldCurrencyMock, context.Signer);
                return states;
            }

            if (!states.TryGetAgentAvatarStates(context.Signer, AvatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }

            var redeemState = states.GetRedeemCodeState();
            if (redeemState is null)
            {
                return states;
            }

            int redeemId;
            try
            {
                redeemId = redeemState.Redeem(Code, AvatarAddress);
            }
            catch (InvalidRedeemCodeException)
            {
                Log.Error("Invalid Code");
                throw;
            }
            catch (DuplicateRedeemException e)
            {
                Log.Warning(e.Message);
                throw;
            }

            var tableSheets = TableSheets.FromActionContext(context);
            var row = tableSheets.RedeemRewardSheet.Values.First(r => r.Id == redeemId);
            var rewards = row.Rewards;

            var itemSheets = tableSheets.ItemSheet;
            foreach (RedeemRewardSheet.RewardInfo info in row.Rewards)
            {
                switch (info.Type)
                {
                    case RewardType.Item:
                        for (var i = 0; i < info.Quantity; i++)
                        {
                            if (info.ItemId is int itemId)
                            {
                                ItemBase item = ItemFactory.CreateItem(itemSheets[itemId]);
                                // We should fix count as 1 because ItemFactory.CreateItem
                                // will create a new item every time.
                                avatarState.inventory.AddItem(item, 1);
                            }
                        }
                        states = states.SetState(AvatarAddress, avatarState.Serialize());
                        break;
                    case RewardType.Gold:
                        states = states.TransferAsset(
                            GoldCurrencyState.Address,
                            context.Signer,
                            states.GetGoldCurrency(),
                            info.Quantity
                        );
                        break;
                    default:
                        // FIXME: We should raise exception here.
                        break;
                }
            }
            states = states.SetState(RedeemCodeState.Address, redeemState.Serialize());
            states = states.SetState(context.Signer, agentState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [nameof(Code)] = Code.Serialize(),
                [nameof(AvatarAddress)] = AvatarAddress.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Code = (Text) plainValue[nameof(Code)];
            AvatarAddress = plainValue[nameof(AvatarAddress)].ToAddress();
        }
    }
}
