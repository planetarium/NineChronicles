namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Xunit;
    using static SerializeKeys;

    public class GrindingTest
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IAccountStateDelta _initialState;

        public GrindingTest()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _currency = new Currency("CRYSTAL", 18, minters: null);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentState = new AgentState(_agentAddress);
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            _agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new State()
                .SetState(
                    Addresses.GetSheetAddress<CrystalMonsterCollectionMultiplierSheet>(),
                    _tableSheets.CrystalMonsterCollectionMultiplierSheet.Serialize())
                .SetState(
                    Addresses.GetSheetAddress<CrystalEquipmentGrindingSheet>(),
                    _tableSheets.CrystalEquipmentGrindingSheet.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(true, true, 120, true, 1, 0, false, false, 0, 100, 1, null)]
        [InlineData(true, true, 120, true, 1, 2, false, false, 0, 200, 1, null)]
        [InlineData(true, true, 120, true, 1, 2, false, true, 0, 200, 1, null)]
        // Check multiplier by monster collection level.
        [InlineData(true, true, 120, true, 1, 0, false, true, 3, 3100, 1, null)]
        [InlineData(true, true, 120, true, 1, 2, false, true, 4, 8200, 1, null)]
        // Invalid equipment count.
        [InlineData(true, true, 120, true, 1, 2, false, true, 4, 280, 0, typeof(InvalidItemCountException))]
        [InlineData(true, true, 120, true, 1, 2, false, true, 4, 280, 11, typeof(InvalidItemCountException))]
        // AgentState not exist.
        [InlineData(false, true, 120, false, 1, 0, false, false, 0, 0, 1, typeof(FailedLoadStateException))]
        // AvatarState not exist.
        [InlineData(true, false, 120, false, 1, 0, false, false, 0, 0, 1, typeof(FailedLoadStateException))]
        // Required more ActionPoint.
        [InlineData(true, true, 0, false, 1, 0, false, false, 0, 0, 1, typeof(NotEnoughActionPointException))]
        // Equipment not exist.
        [InlineData(true, true, 120, false, 1, 0, false, false, 0, 0, 1, typeof(ItemDoesNotExistException))]
        // Locked equipment.
        [InlineData(true, true, 120, true, 100, 0, false, false, 0, 0, 1, typeof(RequiredBlockIndexException))]
        // Equipped equipment.
        [InlineData(true, true, 120, true, 1, 0, true, false, 0, 100, 1, typeof(InvalidEquipmentException))]
        public void Execute(
            bool agentExist,
            bool avatarExist,
            int ap,
            bool equipmentExist,
            long requiredBlockIndex,
            int itemLevel,
            bool equipped,
            bool monsterCollect,
            int monsterCollectLevel,
            int totalAsset,
            int equipmentCount,
            Type exc
        )
        {
            var state = _initialState;
            if (agentExist)
            {
                state = state.SetState(_agentAddress, _agentState.Serialize());
            }

            if (avatarExist)
            {
                _avatarState.actionPoint = ap;

                if (equipmentExist)
                {
                    var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
                    var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, requiredBlockIndex, itemLevel);
                    equipment.equipped = equipped;
                    _avatarState.inventory.AddItem(equipment, count: 1);
                }
                else
                {
                    var row = _tableSheets.ConsumableItemSheet.Values.First(r => r.Grade == 1);
                    var consumable = (Consumable)ItemFactory.CreateItemUsable(row, default, requiredBlockIndex, itemLevel);
                    _avatarState.inventory.AddItem(consumable, count: 1);
                }

                state = state
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize())
                    .SetState(_avatarAddress, _avatarState.SerializeV2());

                Assert.Equal(0 * _currency, state.GetBalance(_avatarAddress, _currency));
            }

            if (monsterCollect)
            {
                var mcAddress = MonsterCollectionState.DeriveAddress(_agentAddress, 0);
                state = state
                    .SetState(
                        mcAddress,
                        new MonsterCollectionState(mcAddress, monsterCollectLevel, 1).Serialize()
                    );
            }

            var equipmentIds = new List<Guid>();
            for (int i = 0; i < equipmentCount; i++)
            {
                equipmentIds.Add(default);
            }

            Assert.Equal(equipmentCount, equipmentIds.Count);

            var action = new Grinding
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = equipmentIds,
            };

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });

                var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
                FungibleAssetValue asset = totalAsset * _currency;

                Assert.Equal(asset, nextState.GetBalance(_avatarAddress, _currency));
                Assert.False(nextAvatarState.inventory.HasNonFungibleItem(default));
                Assert.Equal(115, nextAvatarState.actionPoint);

                var mail = nextAvatarState.mailBox.OfType<GrindingMail>().First(i => i.id.Equals(action.Id));

                Assert.Equal(1, mail.ItemCount);
                Assert.Equal(asset, mail.Asset);
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                }));
            }
        }

        [Theory]
        [ClassData(typeof(CalculateCrystalData))]
        public void CalculateCrystal((int equipmentId, int level)[] equipmentInfos, int monsterCollectionLevel, int expected)
        {
            var equipmentList = new List<Equipment>();
            foreach (var (equipmentId, level) in equipmentInfos)
            {
                var row = _tableSheets.EquipmentItemSheet[equipmentId];
                var equipment =
                    ItemFactory.CreateItemUsable(row, default, 0, level);
                equipmentList.Add((Equipment)equipment);
            }

            Assert.Equal(
                expected * _currency,
                Grinding.CalculateCrystal(
                    equipmentList,
                    _tableSheets.CrystalEquipmentGrindingSheet,
                    monsterCollectionLevel,
                    _tableSheets.CrystalMonsterCollectionMultiplierSheet
                )
            );
        }

        private class CalculateCrystalData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    new[]
                    {
                        (10100000, 0),
                        (10100000, 2),
                    },
                    0,
                    300,
                },
                new object[]
                {
                    new[]
                    {
                        (10100000, 1),
                        (10100000, 2),
                    },
                    3,
                    9300, // This value would change by CrystalMonsterCollectionMultiplierSheet.
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
