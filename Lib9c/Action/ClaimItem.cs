using System;
using System.Collections.Immutable;
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
    public class ClaimItem : GameAction, IClaimItem
    {
        private const string ActionTypeText = "claim_item";

        public Address AvatarAddress { get; private set; }
        public FungibleAssetValue Amount { get; private set; }

        public ClaimItem() {}

        public ClaimItem(Address avatarAddress, FungibleAssetValue amount)
        {
            AvatarAddress = avatarAddress;
            Amount = amount;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize())
                .Add(AmountKey, Amount.Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            Amount = plainValue[AmountKey].ToFungibleAssetValue();
        }

        public override IAccount Execute(IActionContext context)
        {
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            var ticker = Amount.Currency.Ticker;
            if (!ticker.StartsWith("it_") ||
                !int.TryParse(ticker.Replace("it_", string.Empty), out var itemId))
            {
                throw new ArgumentException($"Format of Amount currency's ticker is invalid");
            }

            var balance = states.GetBalance(context.Signer, Amount.Currency);
            if (balance < Amount)
            {
                throw new NotEnoughFungibleAssetValueException(
                    context.Signer.ToHex(),
                    Amount.RawValue,
                    balance);
            }
            states = states.TransferAsset(context, context.Signer, Addresses.ClaimItem, Amount);

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

            var item = itemSheet[itemId] switch
            {
                MaterialItemSheet.Row materialRow => ItemFactory.CreateTradableMaterial(materialRow),
                var itemRow => ItemFactory.CreateItem(itemRow, context.Random)
            };

            avatarState.inventory.AddItem(item, (int)Amount.RawValue);

            return states.SetState(avatarState.address, avatarState.Serialize());
        }
    }
}
