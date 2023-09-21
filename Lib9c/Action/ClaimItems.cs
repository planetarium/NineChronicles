using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private const string ActionTypeText = "claim_items";

        public IEnumerable<Address> AvatarAddresses { get; private set; }
        public IEnumerable<FungibleAssetValue> Amounts { get; private set; }

        public ClaimItems() {}

        public ClaimItems(IEnumerable<Address> avatarAddresses, IEnumerable<FungibleAssetValue> amounts)
        {
            AvatarAddresses = avatarAddresses;
            Amounts = amounts;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, new List(AvatarAddresses.Select(x => x.Serialize())))
                .Add(AmountKey, new List(Amounts.Select(x => x.Serialize())));

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddresses = ((List)plainValue[AvatarAddressKey]).Select(x => x.ToAddress());
            Amounts = ((List)plainValue[AmountKey]).Select(x => x.ToFungibleAssetValue());
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            var states = context.PreviousState;
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();
            var inventoryAddresses =
                AvatarAddresses.Select(avatarAddress => avatarAddress.Derive(LegacyInventoryKey)).ToList();

            var inventories = inventoryAddresses.Select(inventoryAddress =>
            {
                var addressHex = GetSignerAndOtherAddressesHex(context, inventoryAddress);
                return states.GetInventory(inventoryAddress)
                    ?? throw new FailedLoadStateException(ActionTypeText, addressHex, typeof(AvatarState), inventoryAddress);
            }).ToList();

            foreach (var fungibleAssetValue in Amounts)
            {
                var ticker = fungibleAssetValue.Currency.Ticker;
                if (!ticker.StartsWith("IT_") ||
                    !int.TryParse(ticker.Replace("IT_", string.Empty), out var itemId))
                {
                    throw new ArgumentException($"Format of Amount currency's ticker is invalid");
                }

                var decimalPlaces = fungibleAssetValue.Currency.DecimalPlaces;
                if (decimalPlaces != 0)
                {
                    throw new ArgumentException(
                        "DecimalPlaces of fungibleAssetValue for claimItems are not 0");
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

                foreach (var inventory in inventories)
                {
                    inventory.AddItem(item, (int)fungibleAssetValue.RawValue);
                }

                states = states.BurnAsset(context, context.Signer, fungibleAssetValue * inventories.Count);
            }

            foreach (var (inventoryAddress, i) in inventoryAddresses.Select((x, i) => (x, i)))
            {
                states = states.SetState(inventoryAddress, inventories[i].Serialize());
            }

            return states;
        }
    }
}
