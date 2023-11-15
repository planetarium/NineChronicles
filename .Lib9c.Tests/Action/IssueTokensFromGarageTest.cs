namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class IssueTokensFromGarageTest
    {
        // {Id: 100000, Grade: 1, ItemType: Material, ItemSubType: NormalMaterial, ElementalType: Normal, ItemId: baa2081d3b485ef2906c95a3965531ec750a74cfaefe91d0c3061865608b426c}
        private static readonly HashDigest<SHA256> SampleFungibleId =
            HashDigest<SHA256>.FromString("baa2081d3b485ef2906c95a3965531ec750a74cfaefe91d0c3061865608b426c");

        private readonly IAccount _prevState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;

        public IssueTokensFromGarageTest()
        {
            _signer = new PrivateKey().ToAddress();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var garageBalanceAddr = Addresses.GetGarageBalanceAddress(_signer);
            _prevState = new Account(
                MockState.Empty
                    .SetBalance(garageBalanceAddr, Currencies.Crystal * 1000)
            );

            IEnumerable<Material> materials = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateMaterial);
            foreach (Material material in materials)
            {
                var garageAddr = Addresses.GetGarageAddress(_signer, material.FungibleId);
                var garage = new FungibleItemGarage(material, 1000);
                _prevState = _prevState.SetState(garageAddr, garage.Serialize());
            }

            foreach (var (key, value) in sheets)
            {
                _prevState = _prevState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void PlainValue()
        {
            var specs = new List<IssueTokensFromGarage.Spec>()
            {
                IssueTokensFromGarage.Spec.FromFungibleAssetValue(Currencies.Crystal * 1000),
                IssueTokensFromGarage.Spec.FromFungibleItemValue(new FungibleItemValue(SampleFungibleId, 42)),
            };

            var action = new IssueTokensFromGarage(specs);
            Dictionary expected = Dictionary.Empty
                .Add("type_id", "issue_tokens_from_garage")
                .Add("values", List.Empty
                    .Add(new List((Currencies.Crystal * 1000).Serialize(), default(Null)))
                    .Add(new List(default(Null), new FungibleItemValue(SampleFungibleId, 42).Serialize())));
            Assert.Equal(
                expected,
                action.PlainValue
            );
        }

        [Fact]
        public void LoadPlainValue()
        {
            Dictionary encoded = Dictionary.Empty
                .Add("type_id", "issue_tokens_from_garage")
                .Add("values", List.Empty
                    .Add(new List((Currencies.Crystal * 1000).Serialize(), default(Null)))
                    .Add(new List(default(Null), new FungibleItemValue(SampleFungibleId, 42).Serialize())));
            var action = new IssueTokensFromGarage();
            action.LoadPlainValue(encoded);
            var expected = new List<IssueTokensFromGarage.Spec>()
            {
                IssueTokensFromGarage.Spec.FromFungibleAssetValue(Currencies.Crystal * 1000),
                IssueTokensFromGarage.Spec.FromFungibleItemValue(new FungibleItemValue(SampleFungibleId, 42)),
            };

            Assert.Equal(expected, action.Specs);
        }

        [Fact]
        public void Execute_With_FungibleAssetValue()
        {
            var action = new IssueTokensFromGarage(new[]
            {
                IssueTokensFromGarage.Spec.FromFungibleAssetValue(Currencies.Crystal * 42),
            });

            IAccount nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _signer,
                    Rehearsal = false,
                    BlockIndex = 42,
                }
            );

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);

            Assert.Equal(wrappedCrystal * 42, nextState.GetBalance(_signer, wrappedCrystal));
            Assert.Equal(
                Currencies.Crystal * (1000 - 42),
                nextState.GetBalance(
                    Addresses.GetGarageBalanceAddress(_signer),
                    Currencies.Crystal
                )
            );
        }

        [Fact]
        public void Execute_With_FungibleItemValue()
        {
            var action = new IssueTokensFromGarage(new[]
            {
                IssueTokensFromGarage.Spec.FromFungibleItemValue(new FungibleItemValue(SampleFungibleId, 42)),
            });

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _signer,
                    Rehearsal = false,
                    BlockIndex = 42,
                }
            );

            Currency itemCurrency = Currency.Legacy("Item_NT_100000", 0, null);
            var garageAddr = Addresses.GetGarageAddress(_signer, SampleFungibleId);
            Assert.Equal(itemCurrency * 42, nextState.GetBalance(_signer, itemCurrency));
            Assert.Equal(1000 - 42, new FungibleItemGarage(nextState.GetState(garageAddr)).Count);
        }
    }
}
