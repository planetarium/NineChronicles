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
using Nekoyume.Action;
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

        public IEnumerable<Address> AvatarAddresses { get; private set; }
        public IEnumerable<FungibleAssetValue> Amounts { get; private set; }

        public ClaimItems()
        {
        }

        public ClaimItems(IEnumerable<Address> avatarAddresses,
            IEnumerable<FungibleAssetValue> amounts)
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
            states = BurnAssets(context, states);

            var items = BuildItems(context, states);
            var inventoryDictionary = BuildInventoryDictionary(context, states);

            foreach (var (item, amount) in items)
            {
                foreach (var inventory in inventoryDictionary.Values)
                {
                    inventory.AddItem(item, amount);
                }
            }

            return inventoryDictionary
                .OrderBy(keyValuePair => keyValuePair.Key)
                .Aggregate(states, (current, inventoryKeyValue) => current.SetState(
                    inventoryKeyValue.Key,
                    inventoryKeyValue.Value.Serialize()));
        }

        private IAccount BurnAssets(IActionContext context, IAccount states)
        {
            foreach (var fungibleAssetValue in Amounts)
            {
                var decimalPlaces = fungibleAssetValue.Currency.DecimalPlaces;
                if (decimalPlaces != 0)
                {
                    throw new ArgumentException(
                        "DecimalPlaces of fungibleAssetValue for claimItems are not 0");
                }

                states = states.BurnAsset(context, context.Signer,
                    fungibleAssetValue * AvatarAddresses.Count());
            }

            return states;
        }

        private List<(ItemBase item, int amount)> BuildItems(IActionContext context,
            IAccount states)
        {
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();

            return Amounts.Select(fungibleAssetValue =>
            {
                var ticker = fungibleAssetValue.Currency.Ticker;
                if (!ticker.StartsWith("IT_") ||
                    !int.TryParse(ticker.Replace("IT_", string.Empty), out var itemId))
                {
                    throw new ArgumentException($"Format of Amount currency's ticker is invalid");
                }

                var item = itemSheet[itemId] switch
                {
                    MaterialItemSheet.Row materialRow =>
                        ItemFactory.CreateTradableMaterial(materialRow),
                    var itemRow => ItemFactory.CreateItem(itemRow, context.Random)
                };

                return (item, 0);
            }).ToList();
        }

        private Dictionary<Address, Inventory> BuildInventoryDictionary(
            IActionContext context,
            IAccountState states)
        {
            return AvatarAddresses
                .Select(avatarAddress => avatarAddress.Derive(LegacyInventoryKey))
                .ToDictionary(
                    inventoryAddress => inventoryAddress,
                    inventoryAddress => states.GetInventory(inventoryAddress) ??
                                        throw new FailedLoadStateException(
                                            ActionTypeText,
                                            GetSignerAndOtherAddressesHex(context, inventoryAddress),
                                            typeof(Inventory),
                                            inventoryAddress));
        }
    }
}
