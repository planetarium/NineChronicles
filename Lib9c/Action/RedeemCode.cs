using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/861
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("redeem_code3")]
    public class RedeemCode : GameAction, IRedeemCodeV1
    {
        public string Code { get; internal set; }

        public Address AvatarAddress {get; internal set; }

        string IRedeemCodeV1.Code => Code;
        Address IRedeemCodeV1.AvatarAddress => AvatarAddress;

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
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(RedeemCodeState.Address, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RedeemCode exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out AvatarState avatarState, out _))
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
                Log.Error("{AddressesHex}Invalid Code", addressesHex);
                throw;
            }
            catch (DuplicateRedeemException e)
            {
                Log.Warning("{AddressesHex}{Message}", addressesHex, e.Message);
                throw;
            }

            var row = states.GetSheet<RedeemRewardSheet>().Values.First(r => r.Id == redeemId);
            var itemSheets = states.GetItemSheet();

            foreach (RedeemRewardSheet.RewardInfo info in row.Rewards)
            {
                switch (info.Type)
                {
                    case RewardType.Item:
                        for (var i = 0; i < info.Quantity; i++)
                        {
                            if (info.ItemId is int itemId)
                            {
                                ItemBase item = ItemFactory.CreateItem(itemSheets[itemId], context.Random);
                                // We should fix count as 1 because ItemFactory.CreateItem
                                // will create a new item every time.
                                avatarState.inventory.AddItem(item, count: 1);
                            }
                        }
                        break;
                    case RewardType.Gold:
                        states = states.TransferAsset(
                            GoldCurrencyState.Address,
                            context.Signer,
                            states.GetGoldCurrency() * info.Quantity
                        );
                        break;
                    default:
                        // FIXME: We should raise exception here.
                        break;
                }
            }
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RedeemCode Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(RedeemCodeState.Address, redeemState.Serialize());
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
