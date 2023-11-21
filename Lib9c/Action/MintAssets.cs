#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class MintAssets : ActionBase
    {
        public const string TypeIdentifier = "mint_assets";

        public MintAssets()
        {
        }

        public MintAssets(IEnumerable<MintSpec> specs, string? memo)
        {
            MintSpecs = specs.ToList();
            Memo = memo;
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            if (MintSpecs is null)
            {
                throw new InvalidOperationException();
            }

            IAccount state = context.PreviousState;
            HashSet<Address> allowed = new();

            if (state.TryGetState(Addresses.Admin, out Dictionary rawDict))
            {
                allowed.Add(new AdminState(rawDict).AdminAddress);
            }

            if (state.TryGetState(Addresses.AssetMinters, out List minters))
            {
                allowed.UnionWith(minters.Select(m => m.ToAddress()));
            }

            if (!allowed.Contains(context.Signer))
            {
                throw new InvalidMinterException(context.Signer);
            }

            Dictionary<Address, (List<FungibleAssetValue>, List<FungibleItemValue>)> mailRecords = new();

            foreach (var (recipient, assets, items) in MintSpecs)
            {
                if (!mailRecords.TryGetValue(recipient, out var records))
                {
                    mailRecords[recipient] = records = new(
                        new List<FungibleAssetValue>(),
                        new List<FungibleItemValue>()
                    );
                }

                (List<FungibleAssetValue> favs, List<FungibleItemValue> fivs) = records;

                if (assets is { } assetsNotNull)
                {
                    state = state.MintAsset(context, recipient, assetsNotNull);
                    favs.Add(assetsNotNull);
                }

                if (items is { } itemsNotNull)
                {
                    Address inventoryAddr = recipient.Derive(SerializeKeys.LegacyInventoryKey);
                    Inventory inventory = state.GetInventory(inventoryAddr);
                    MaterialItemSheet itemSheet = state.GetSheet<MaterialItemSheet>();
                    if (itemSheet is null || itemSheet.OrderedList is null)
                    {
                        throw new InvalidOperationException();
                    }

                    foreach (MaterialItemSheet.Row row in itemSheet.OrderedList)
                    {
                        if (row.ItemId.Equals(itemsNotNull.Id))
                        {
                            Material item = ItemFactory.CreateMaterial(row);
                            inventory.AddFungibleItem(item, itemsNotNull.Count);
                        }
                    }

                    state = state.SetState(inventoryAddr, inventory.Serialize());
                    fivs.Add(itemsNotNull);
                }
            }

            IRandom rng = context.GetRandom();
            foreach (var recipient in mailRecords.Keys)
            {
                if (
                    state.GetState(recipient) is Dictionary dict &&
                    dict.TryGetValue((Text)SerializeKeys.MailBoxKey, out IValue rawMailBox)
                )
                {
                    var mailBox = new MailBox((List)rawMailBox);
                    (List<FungibleAssetValue> favs, List<FungibleItemValue> fivs) = mailRecords[recipient];
                    mailBox.Add(
                        new UnloadFromMyGaragesRecipientMail(
                            context.BlockIndex,
                            rng.GenerateRandomGuid(),
                            context.BlockIndex,
                            favs.Select(v => (recipient, v)),
                            fivs.Select(v => (v.Id, v.Count)),
                            Memo
                        )
                    );
                    mailBox.CleanUp();
                    dict = dict.SetItem(SerializeKeys.MailBoxKey, mailBox.Serialize());
                    state = state.SetState(recipient, dict);
                }
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            var asList = (List)asDict["values"];

            if (asList[0] is Text memo)
            {
                Memo = memo;
            }
            else
            {
                Memo = null;
            }

            MintSpecs = asList.Skip(1).Select(v =>
            {
                return new MintSpec((List)v);
            }).ToList();
        }

        public override IValue PlainValue
        {
            get
            {
                var values = new List<IValue>
                {
                    Memo is { } memoNotNull ? (Text)memoNotNull : Null.Value
                };
                if (MintSpecs is { } mintSpecsNotNull)
                {
                    values.AddRange(mintSpecsNotNull.Select(s => s.Serialize()));
                }

                return new Dictionary(
                    new[]
                    {
                        new KeyValuePair<IKey, IValue>((Text)"type_id", (Text)TypeIdentifier),
                        new KeyValuePair<IKey, IValue>((Text)"values", new List(values))
                    }
                );
            }
        }

        public List<MintSpec>? MintSpecs
        {
            get;
            private set;
        }

        public string? Memo { get; private set; }

        public readonly struct MintSpec
        {
            public MintSpec(List bencoded)
                : this(
                    bencoded[0].ToAddress(),
                    bencoded[1] is List rawAssets ? rawAssets.ToFungibleAssetValue() : null,
                    bencoded[2] is List rawItems ? new FungibleItemValue(rawItems) : null
                )
            {
            }

            public MintSpec(
                Address recipient,
                FungibleAssetValue? assets,
                FungibleItemValue? items
            )
            {
                Recipient = recipient;
                Assets = assets;
                Items = items;
            }

            public IValue Serialize() => new List(
                Recipient.Serialize(),
                Assets?.Serialize() ?? Null.Value,
                Items?.Serialize() ?? Null.Value
            );

            internal void Deconstruct(
                out Address recipient,
                out FungibleAssetValue? assets,
                out FungibleItemValue? items
            )
            {
                recipient = Recipient;
                assets = Assets;
                items = Items;
            }

            public Address Recipient { get; }
            public FungibleAssetValue? Assets { get; }
            public FungibleItemValue? Items { get; }

        }
    }
}
