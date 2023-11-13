using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Garages;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType(ActionTypeText)]
    public class ClaimItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "claim_items";
        private const int MaxClaimDataCount = 100;

        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; private set; }
        public string Memo;

        public ClaimItems()
        {
        }

        public ClaimItems(IReadOnlyList<(Address, IReadOnlyList<FungibleAssetValue>)> claimData, string memo = null)
        {
            ClaimData = claimData;
            Memo = memo;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            GetPlainValueInternal();

        private IImmutableDictionary<string, IValue> GetPlainValueInternal()
        {
            var dict = ImmutableDictionary<string, IValue>.Empty
                .Add(ClaimDataKey, ClaimData.Aggregate(List.Empty, (list, tuple) =>
                {
                    var serializedFungibleAssetValues = tuple.fungibleAssetValues.Select(x => x.Serialize()).Serialize();

                    return list.Add(new List(tuple.address.Bencoded, serializedFungibleAssetValues));
                }));
            if (!string.IsNullOrEmpty(Memo))
            {
                dict = dict.Add(MemoKey, Memo.Serialize());
            }

            return dict;
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            ClaimData = ((List)plainValue[ClaimDataKey])
                .Select(pairValue =>
                {
                    List pair = (List)pairValue;
                    return (
                        new Address(pair[0]),
                        pair[1].ToList(x => x.ToFungibleAssetValue()) as IReadOnlyList<FungibleAssetValue>);
                }).ToList();
            if (plainValue.ContainsKey(MemoKey))
            {
                Memo = (Text) plainValue[MemoKey];
            }
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            if (ClaimData.Count > MaxClaimDataCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ClaimData),
                    ClaimData.Count,
                    $"ClaimData should be less than {MaxClaimDataCount}");
            }

            var states = context.PreviousState;
            var random = context.GetRandom();
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();

            foreach (var (recipientAddress, fungibleAssetValues) in ClaimData)
            {
                var inventoryAddress = recipientAddress.Derive(LegacyInventoryKey);
                Inventory inventory = null;
                bool inventoryExist = false;
                try
                {
                    inventory = states.GetInventory(inventoryAddress);
                    inventoryExist = true;
                }
                catch (Exception)
                {
                    // inventory not required if claim fav
                }

                foreach (var fungibleAssetValue in fungibleAssetValues)
                {
                    var wrappedTicker = fungibleAssetValue.Currency.Ticker;
                    var separator = wrappedTicker.Contains("__") ? "__" : "_";
                    var parsedTicker = wrappedTicker.Split(separator);
                    if (parsedTicker.Length == 2)
                    {
                        var ticker = parsedTicker[1];
                        var currency = Currencies.GetMinterlessCurrency(ticker);
                        states = states
                            .BurnAsset(context, context.Signer, fungibleAssetValue)
                            .MintAsset(context, recipientAddress, FungibleAssetValue.FromRawValue(currency, fungibleAssetValue.RawValue));
                    }
                    else
                    {
                        if (!inventoryExist)
                        {
                            throw new FailedLoadStateException(
                                ActionTypeText,
                                GetSignerAndOtherAddressesHex(context, inventoryAddress),
                                typeof(Inventory),
                                inventoryAddress);
                        }
                        if (fungibleAssetValue.Currency.DecimalPlaces != 0)
                        {
                            throw new ArgumentException(
                                $"DecimalPlaces of fungibleAssetValue for claimItems are not 0: {fungibleAssetValue.Currency.Ticker}");
                        }

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
                            var itemRow => ItemFactory.CreateItem(itemRow, random)
                        };

                        // FIXME: This is an implementation bug in the Inventory class,
                        // but we'll deal with it temporarily here.
                        // If Pluggable AEV ever becomes a reality,
                        // it's only right that this is fixed in Inventory.
                        if (item is INonFungibleItem)
                        {
                            foreach (var _ in Enumerable.Range(0, (int)fungibleAssetValue.RawValue))
                            {
                                inventory.AddItem(item, 1);
                            }
                        }
                        else
                        {
                            inventory.AddItem(item, (int)fungibleAssetValue.RawValue);
                        }
                    }
                }

                if (inventoryExist)
                {
                    states = states.SetState(inventoryAddress, inventory.Serialize());
                }
            }

            return states;
        }
    }
}
