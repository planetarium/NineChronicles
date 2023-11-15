using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Garages;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Org.BouncyCastle.Asn1.Esf;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class IssueTokensFromGarage : ActionBase
    {
        public const string TypeIdentifier = "issue_tokens_from_garage";

        public IssueTokensFromGarage()
        {
        }

        public IssueTokensFromGarage(IEnumerable<Spec> specs)
        {
            Specs = specs.ToList();
        }

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"type_id", (Text)TypeIdentifier),
                    new KeyValuePair<IKey, IValue>(
                        (Text)"values",
                        Specs is { }
                            ? new List(Specs.Select(s => s.Serialize()))
                            : Null.Value
                    )
                }
            );

        public IEnumerable<Spec> Specs { get; private set; }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            if (Specs is null)
            {
                throw new InvalidOperationException();
            }

            IAccount state = context.PreviousState;
            Address garageBalanceAddress = Addresses.GetGarageBalanceAddress(context.Signer);

            foreach (var (assets, items) in Specs)
            {
                if (assets is { } assetsNotNull)
                {
                    state = state.BurnAsset(
                        context,
                        garageBalanceAddress,
                        assetsNotNull
                    );
                    Currency wrappedCurrency = Currencies.GetWrappedCurrency(assetsNotNull.Currency);
                    state = state.MintAsset(
                        context,
                        context.Signer,
                        FungibleAssetValue.FromRawValue(wrappedCurrency, assetsNotNull.RawValue)
                    );
                }

                if (items is { } itemsNotNull)
                {
                    var tuples = GarageUtils.WithGarageTuples(
                        context.Signer,
                        state,
                        new[] { (itemsNotNull.Id, itemsNotNull.Count) }
                    );
                    foreach (var (_, count, garageAddr, garage) in tuples)
                    {
                        if (garage.Item is Material material)
                        {
                            garage.Unload(count);
                            state = state.SetState(garageAddr, garage.Serialize());
                            var currency = Currencies.GetItemCurrency(material.Id, false);
                            state = state.MintAsset(
                                context,
                                context.Signer,
                                currency * count
                            );
                        }
                    }
                }
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            Specs = ((List)asDict["values"]).Select(v =>
            {
                return new Spec((List)v);
            }).ToList();
        }

        public readonly struct Spec
        {
            public Spec(List bencoded)
                : this(
                    bencoded[0] is List rawAssets ? rawAssets.ToFungibleAssetValue() : null,
                    bencoded[1] is List rawItems ? new FungibleItemValue(rawItems) : null
                )
            {
            }

            private Spec(FungibleAssetValue? assets, FungibleItemValue? items)
            {
                Assets = assets;
                Items = items;
            }

            public static Spec FromFungibleAssetValue(FungibleAssetValue assets) =>
                new(assets, null);

            public static Spec FromFungibleItemValue(FungibleItemValue items) =>
                new(null, items);

            public IValue Serialize() => new List(
                Assets?.Serialize() ?? Null.Value,
                Items?.Serialize() ?? Null.Value
            );

            internal void Deconstruct(
                out FungibleAssetValue? assets,
                out FungibleItemValue? items
            )
            {
                assets = Assets;
                items = Items;
            }

            public FungibleAssetValue? Assets { get; }

            public FungibleItemValue? Items { get; }
        }
    }
}
