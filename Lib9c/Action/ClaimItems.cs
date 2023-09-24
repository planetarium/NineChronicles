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
    [ActionType(ActionTypeText)]
    public class ClaimItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "claim_items";

        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; private set; }

        public ClaimItems()
        {
        }

        public ClaimItems(IReadOnlyList<(Address, IReadOnlyList<FungibleAssetValue>)> claimData)
        {
            ClaimData = claimData;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(ClaimDataKey, ClaimData.Select(tuple =>
                {
                    var serializedFungibleAssetValues = tuple.fungibleAssetValues.Select(x => x.Serialize()).Serialize();

                    return (tuple.address, serialized: serializedFungibleAssetValues);
                }).Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            ClaimData = plainValue[ClaimDataKey].ToStateList()
                .Select((tuple =>
                {
                    return (
                        tuple.Item1,
                        tuple.Item2.ToList((x => x.ToFungibleAssetValue())) as IReadOnlyList<FungibleAssetValue>);
                })).ToList();
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            var states = context.PreviousState;
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();

            foreach (var (avatarAddress, fungibleAssetValues) in ClaimData)
            {
                var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
                var inventory = states.GetInventory(inventoryAddress)
                                ?? throw new FailedLoadStateException(
                                    ActionTypeText,
                                    GetSignerAndOtherAddressesHex(context, inventoryAddress),
                                    typeof(Inventory),
                                    inventoryAddress);

                foreach (var fungibleAssetValue in fungibleAssetValues)
                {
                    if (fungibleAssetValue.Currency.DecimalPlaces != 0)
                    {
                        throw new ArgumentException(
                            $"DecimalPlaces of fungibleAssetValue for claimItems are not 0: {fungibleAssetValue.Currency.Ticker}");
                    }

                    var parsedTicker = fungibleAssetValue.Currency.Ticker.Split("_");
                    if (parsedTicker.Length != 3
                        || parsedTicker[0] != "Item"
                        || (parsedTicker[1] != "NT" && parsedTicker[1] != "T")
                        || !int.TryParse(parsedTicker[2], out var itemId))
                    {
                        throw new ArgumentException(
                            $"Format of Amount currency's ticker is invalid");
                    }

                    states = states.BurnAsset(context, context.Signer, fungibleAssetValue);

                    var item = itemSheet[itemId] switch
                    {
                        MaterialItemSheet.Row materialRow => parsedTicker[1] == "T"
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateMaterial(materialRow),
                        var itemRow => ItemFactory.CreateItem(itemRow, context.Random)
                    };

                    inventory.AddItem(item, (int)fungibleAssetValue.RawValue);
                }

                states = states.SetState(inventoryAddress, inventory.Serialize());
            }

            return states;
        }
    }
}
