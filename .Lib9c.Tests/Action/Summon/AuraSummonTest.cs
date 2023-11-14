namespace Lib9c.Tests.Action.Summon
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Lib9c.Tests.Fixtures.TableCSV.Summon;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Summon;
    using Xunit;
    using static SerializeKeys;

    public class AuraSummonTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private TableSheets _tableSheets;
        private IAccount _initialState;

        public AuraSummonTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var gold = new GoldCurrencyState(_currency);

            var context = new ActionContext();
            _initialState = new Account(MockState.Empty)
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(context, GoldCurrencyState.Address, gold.Currency * 100000000000)
                .TransferAsset(
                    context,
                    Addresses.GoldCurrency,
                    _agentAddress,
                    gold.Currency * 1000
                );

            Assert.Equal(
                gold.Currency * 99999999000,
                _initialState.GetBalance(Addresses.GoldCurrency, gold.Currency)
            );
            Assert.Equal(
                gold.Currency * 1000,
                _initialState.GetBalance(_agentAddress, gold.Currency)
            );

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData("V1", 10001)]
        [InlineData("V1", 10002)]
        [InlineData("V2", 10001)]
        [InlineData("V2", 10002)]
        public void CumulativeRatio(string version, int groupId)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            if (version == "V1")
            {
                sheets[nameof(SummonSheet)] = SummonSheetFixtures.V1;
            }
            else
            {
                sheets[nameof(SummonSheet)] = SummonSheetFixtures.V2;
            }

            _tableSheets = new TableSheets(sheets);
            var sheet = _tableSheets.SummonSheet;

            var targetRow = sheet.OrderedList.First(r => r.GroupId == groupId);

            for (var i = 1; i <= SummonSheet.Row.MaxRecipeCount; i++)
            {
                var sum = 0;
                for (var j = 0; j < i; j++)
                {
                    if (j < targetRow.Recipes.Count)
                    {
                        sum += targetRow.Recipes[j].Item2;
                    }
                }

                Assert.Equal(sum, targetRow.CumulativeRatio(i));
            }
        }

        [Theory]
        // success first group
        [InlineData("V1", 10001, 1, 800201, 1, 1, new[] { 10610000 }, null)]
        [InlineData("V1", 10001, 2, 800201, 2, 54, new[] { 10620000, 10630000 }, null)]
        // success second group
        [InlineData("V1", 10002, 1, 600201, 1, 1, new[] { 10620001 }, null)]
        [InlineData("V1", 10002, 2, 600201, 2, 4, new[] { 10620001, 10630001 }, null)]
        // Nine plus zero
        [InlineData(
            "V1",
            10001,
            9,
            800201,
            9,
            0,
            new[] { 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10620000, 10620000, 10620000 },
            null
        )]
        [InlineData(
            "V1",
            10002,
            9,
            600201,
            9,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10620001, 10630001, 10630001, 10630001, 10630001 },
            null
        )]
        // Ten plus one
        [InlineData(
            "V1",
            10001,
            10,
            800201,
            10,
            0,
            new[] { 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10620000, 10620000, 10620000 },
            null
        )]
        [InlineData(
            "V1",
            10002,
            10,
            600201,
            10,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10620001, 10620001, 10630001, 10620001, 10630001, 10630001, 10630001 },
            null
        )]
        // fail by invalid group
        [InlineData("V1", 100003, 1, null, 0, 0, new int[] { }, typeof(RowNotInTableException))]
        // fail by not enough material
        [InlineData("V1", 10001, 1, 800201, 0, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        [InlineData("V1", 10001, 2, 800201, 0, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        // Fail by exceeding summon limit
        [InlineData("V1", 10001, 11, 800201, 22, 1, new int[] { }, typeof(InvalidSummonCountException))]
        // 15 recipes
        [InlineData("V2", 10002, 1, 600201, 1, 5341, new[] { 10650006 }, null)]
        public void Execute(
            string version,
            int groupId,
            int summonCount,
            int? materialId,
            int materialCount,
            int seed,
            int[] expectedEquipmentId,
            Type expectedExc
        )
        {
            var random = new TestRandom(seed);
            var state = _initialState;
            state = state.SetState(
                Addresses.TableSheet.Derive(nameof(SummonSheet)),
                version == "V1" ? SummonSheetFixtures.V1.Serialize() : SummonSheetFixtures.V2.Serialize()
            );

            if (!(materialId is null))
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount * _tableSheets.SummonSheet[groupId].CostMaterialCount
                );
                state = state
                        .SetState(_avatarAddress, _avatarState.SerializeV2())
                        .SetState(
                            _avatarAddress.Derive(LegacyInventoryKey),
                            _avatarState.inventory.Serialize()
                        )
                        .SetState(
                            _avatarAddress.Derive(LegacyWorldInformationKey),
                            _avatarState.worldInformation.Serialize()
                        )
                        .SetState(
                            _avatarAddress.Derive(LegacyQuestListKey),
                            _avatarState.questList.Serialize()
                        )
                    ;
            }

            var action = new AuraSummon(
                _avatarAddress,
                groupId,
                summonCount
            );

            if (expectedExc == null)
            {
                // Success
                var ctx = new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                };
                ctx.SetRandom(random);
                var nextState = action.Execute(ctx);

                var equipments = nextState.GetAvatarStateV2(_avatarAddress).inventory.Equipments
                    .ToList();
                Assert.Equal(expectedEquipmentId.Length, equipments.Count);

                var checkedEquipments = new List<Guid>();
                foreach (var equipmentId in expectedEquipmentId)
                {
                    var resultEquipment = equipments.First(e =>
                        e.Id == equipmentId && !checkedEquipments.Contains(e.ItemId)
                    );

                    checkedEquipments.Add(resultEquipment.ItemId);
                    Assert.NotNull(resultEquipment);
                    Assert.Equal(1, resultEquipment.RequiredBlockIndex);
                    Assert.True(resultEquipment.optionCountFromCombination > 0);
                }

                nextState.GetAvatarStateV2(_avatarAddress).inventory
                    .TryGetItem((int)materialId!, out var resultMaterial);
                Assert.Equal(0, resultMaterial?.count ?? 0);
            }
            else
            {
                // Failure
                Assert.Throws(expectedExc, () =>
                {
                    action.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        BlockIndex = 1,
                        RandomSeed = random.Seed,
                    });
                });
            }
        }
    }
}
