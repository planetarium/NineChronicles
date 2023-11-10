#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
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

        public MintAssets(IEnumerable<MintSpec> specs)
        {
            MintSpecs = specs.ToList();
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            if (MintSpecs is null)
            {
                throw new InvalidOperationException();
            }

            CheckPermission(context);
            IAccount state = context.PreviousState;

            foreach (var (recipient, assets, items) in MintSpecs)
            {
                if (assets is { } assetsNotNull)
                {
                    state = state.MintAsset(context, recipient, assetsNotNull);
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
                }

            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            MintSpecs = ((List)asDict["values"]).Select(v =>
            {
                return new MintSpec((List)v);
            }).ToList();
        }

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"type_id", (Text)TypeIdentifier),
                    new KeyValuePair<IKey, IValue>(
                        (Text)"values",
                        MintSpecs is { }
                            ? new List(MintSpecs.Select(s => s.Serialize()))
                            : Null.Value
                    )
                }
            );

        public List<MintSpec>? MintSpecs
        {
            get;
            private set;
        }

        public readonly struct FungibleItemValue
        {
            public FungibleItemValue(List bencoded)
                : this(
                    new HashDigest<SHA256>((Binary)bencoded[0]),
                    (Integer)bencoded[1]
                )
            {
            }

            public FungibleItemValue(HashDigest<SHA256> id, int count)
            {
                Id = id;
                Count = count;
            }

            public IValue Serialize()
            {
                return new List(Id.Serialize(), (Integer)Count);
            }

            public HashDigest<SHA256> Id { get; }
            public int Count { get; }
        }

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
