using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public class ClaimItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "claim_item";

        public Address AvatarAddress { get; private set; }
        public IEnumerable<FungibleAssetValue> Amounts { get; private set; }

        public ClaimItems() {}

        public ClaimItems(Address avatarAddress, IEnumerable<FungibleAssetValue> amounts)
        {
            AvatarAddress = avatarAddress;
            Amounts = amounts;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize())
                .Add(AmountKey, new List(Amounts.Select(x => x.Serialize())));

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            Amounts = ((List)plainValue[AmountKey]).Select(x => x.ToFungibleAssetValue());
        }

        public override IAccount Execute(IActionContext context)
        {
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState is null)
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            var itemSheet = states.GetSheets(sheetTypes: new[]
            {
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            }).GetItemSheet();

            foreach (var fungibleAssetValue in Amounts)
            {
                var ticker = fungibleAssetValue.Currency.Ticker;
                if (!ticker.StartsWith("it_") ||
                    !int.TryParse(ticker.Replace("it_", string.Empty), out var itemId))
                {
                    throw new ArgumentException($"Format of Amount currency's ticker is invalid");
                }

                var balance = states.GetBalance(context.Signer, fungibleAssetValue.Currency);
                if (balance < fungibleAssetValue)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        context.Signer.ToHex(),
                        fungibleAssetValue.RawValue,
                        balance);
                }

                var item = itemSheet[itemId] switch
                {
                    MaterialItemSheet.Row materialRow =>
                        ItemFactory.CreateTradableMaterial(materialRow),
                    var itemRow => ItemFactory.CreateItem(itemRow, context.Random)
                };

                avatarState.inventory.AddItem(item, (int)fungibleAssetValue.RawValue);

                states = states
                    .TransferAsset(context, context.Signer, Addresses.ClaimItem, fungibleAssetValue)
                    .SetState(avatarState.address, avatarState.Serialize());
            }

            return states;
        }
    }
}
