namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Extensions;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Coupons;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class AccountStateViewExtensionsTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;

        public AccountStateViewExtensionsTest()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive(string.Format(CultureInfo.InvariantCulture, CreateAvatar2.DeriveFormat, 0));
            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void TryGetAvatarState()
        {
            var states = new MockStateDelta();
            states = (MockStateDelta)states.SetState(_avatarAddress, _avatarState.Serialize());

            Assert.True(states.TryGetAvatarState(_agentAddress, _avatarAddress, out var avatarState2));
            Assert.Equal(_avatarAddress, avatarState2.address);
            Assert.Equal(_agentAddress, avatarState2.agentAddress);
        }

        [Fact]
        public void TryGetAvatarStateEmptyAddress()
        {
            var states = new MockStateDelta();

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateAddressKeyNotFoundException()
        {
            var states = new MockStateDelta().SetState(default, Dictionary.Empty);

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateKeyNotFoundException()
        {
            var states = new MockStateDelta()
                .SetState(
                default,
                Dictionary.Empty
                    .Add("agentAddress", default(Address).Serialize())
            );

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateInvalidCastException()
        {
            var states = new MockStateDelta().SetState(default, default(Text));

            Assert.False(states.TryGetAvatarState(default, default, out _));
        }

        [Fact]
        public void TryGetAvatarStateInvalidAddress()
        {
            var states = new MockStateDelta().SetState(default, _avatarState.Serialize());

            Assert.False(states.TryGetAvatarState(Addresses.GameConfig, _avatarAddress, out _));
        }

        [Fact]
        public void GetAvatarStateV2()
        {
            var states = new MockStateDelta();
            states = (MockStateDelta)states
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());

            var v2 = states.GetAvatarStateV2(_avatarAddress);
            Assert.NotNull(v2.inventory);
            Assert.NotNull(v2.worldInformation);
            Assert.NotNull(v2.questList);
        }

        [Theory]
        [InlineData(LegacyInventoryKey)]
        [InlineData(LegacyWorldInformationKey)]
        [InlineData(LegacyQuestListKey)]
        public void GetAvatarStateV2_Throw_FailedLoadStateException(string key)
        {
            var states = new MockStateDelta();
            states = (MockStateDelta)states
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            states = (MockStateDelta)states.SetState(_avatarAddress.Derive(key), null);
            var exc = Assert.Throws<FailedLoadStateException>(() => states.GetAvatarStateV2(_avatarAddress));
            Assert.Contains(key, exc.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryGetAvatarStateV2(bool backward)
        {
            var states = new MockStateDelta();
            if (backward)
            {
                states = (MockStateDelta)states
                    .SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                states = (MockStateDelta)states
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            }

            Assert.True(states.TryGetAvatarStateV2(_agentAddress, _avatarAddress, out _, out bool migrationRequired));
            Assert.Equal(backward, migrationRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryGetAgentAvatarStatesV2(bool backward)
        {
            var states = new MockStateDelta().SetState(_agentAddress, _agentState.Serialize());

            if (backward)
            {
                states = (MockStateDelta)states
                    .SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                states = (MockStateDelta)states
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
            }

            Assert.True(states.TryGetAgentAvatarStatesV2(_agentAddress, _avatarAddress, out _, out _, out bool avatarMigrationRequired));
            Assert.Equal(backward, avatarMigrationRequired);
        }

        [Fact]
        public void GetStatesAsDict()
        {
            IAccountStateDelta states = new MockStateDelta();
            var dict = new Dictionary<Address, IValue>
            {
                { new PrivateKey().ToAddress(), Null.Value },
                { new PrivateKey().ToAddress(), new Bencodex.Types.Boolean(false) },
                { new PrivateKey().ToAddress(), new Bencodex.Types.Boolean(true) },
                { new PrivateKey().ToAddress(), new Integer(int.MinValue) },
                { new PrivateKey().ToAddress(), new Integer(0) },
                { new PrivateKey().ToAddress(), new Integer(int.MaxValue) },
            };
            foreach (var (address, value) in dict)
            {
                states = states.SetState(address, value);
            }

            var stateDict = states.GetStatesAsDict(dict.Keys.ToArray());
            foreach (var (address, value) in dict)
            {
                Assert.True(stateDict.ContainsKey(address));
                var innerValue = stateDict[address];
                Assert.Equal(value, innerValue);
            }
        }

        [Fact]
        public void GetSheets()
        {
            IAccountStateDelta states = new MockStateDelta();
            SheetsExtensionsTest.InitSheets(
                states,
                out _,
                out var sheetsAddressAndValues,
                out var sheetTypes,
                out var stateSheets);
            foreach (var sheetType in sheetTypes)
            {
                Assert.True(stateSheets.ContainsKey(sheetType));
                var (address, sheet) = stateSheets[sheetType];
                var expectedAddress = Addresses.TableSheet.Derive(sheetType.Name);
                Assert.Equal(address, expectedAddress);

                var constructor = sheetType.GetConstructor(Type.EmptyTypes);
                Assert.NotNull(constructor);
                var expectedSheet = (ISheet)constructor.Invoke(Array.Empty<object>());
                expectedSheet.Set(sheetsAddressAndValues[address].ToDotnetString());
                Assert.Equal(sheet, expectedSheet);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetCrystalCostState(bool exist)
        {
            IAccountStateDelta state = new MockStateDelta();
            int expectedCount = exist ? 1 : 0;
            FungibleAssetValue expectedCrystal = exist
                ? 100 * CrystalCalculator.CRYSTAL
                : 0 * CrystalCalculator.CRYSTAL;
            Address address = default;
            var crystalCostState = new CrystalCostState(address, expectedCrystal);
            crystalCostState.Count = expectedCount;
            if (exist)
            {
                state = state.SetState(address, crystalCostState.Serialize());
            }

            CrystalCostState actual = state.GetCrystalCostState(address);
            Assert.Equal(expectedCount, actual.Count);
            Assert.Equal(expectedCrystal, actual.CRYSTAL);
        }

        [Theory]
        [InlineData(0L, false)]
        [InlineData(14_400L, false)]
        [InlineData(100_800L, true)]
        [InlineData(151_200L, true)]
        public void GetCrystalCostStates(long blockIndex, bool previousWeeklyExist)
        {
            long interval = _tableSheets.CrystalFluctuationSheet.Values.First(r => r.Type == CrystalFluctuationSheet.ServiceType.Combination).BlockInterval;
            var weeklyIndex = (int)(blockIndex / interval);
            Address dailyCostAddress =
                Addresses.GetDailyCrystalCostAddress((int)(blockIndex / CrystalCostState.DailyIntervalIndex));
            Address weeklyCostAddress = Addresses.GetWeeklyCrystalCostAddress(weeklyIndex);
            Address previousCostAddress = Addresses.GetWeeklyCrystalCostAddress(weeklyIndex - 1);
            Address beforePreviousCostAddress = Addresses.GetWeeklyCrystalCostAddress(weeklyIndex - 2);
            var crystalCostState = new CrystalCostState(default, 100 * CrystalCalculator.CRYSTAL);
            IAccountStateDelta state = new MockStateDelta()
                .SetState(dailyCostAddress, crystalCostState.Serialize())
                .SetState(weeklyCostAddress, crystalCostState.Serialize())
                .SetState(previousCostAddress, crystalCostState.Serialize())
                .SetState(Addresses.GetSheetAddress<CrystalFluctuationSheet>(), _tableSheets.CrystalFluctuationSheet.Serialize())
                .SetState(beforePreviousCostAddress, crystalCostState.Serialize());
            var (daily, weekly, previousWeekly, beforePreviousWeekly) =
                state.GetCrystalCostStates(blockIndex, interval);

            Assert.NotNull(daily);
            Assert.NotNull(weekly);
            Assert.Equal(100 * CrystalCalculator.CRYSTAL, daily.CRYSTAL);
            Assert.Equal(100 * CrystalCalculator.CRYSTAL, weekly.CRYSTAL);
            if (previousWeeklyExist)
            {
                Assert.NotNull(previousWeekly);
                Assert.NotNull(beforePreviousWeekly);
                Assert.Equal(100 * CrystalCalculator.CRYSTAL, previousWeekly.CRYSTAL);
                Assert.Equal(100 * CrystalCalculator.CRYSTAL, beforePreviousWeekly.CRYSTAL);
            }
            else
            {
                Assert.Null(previousWeekly);
                Assert.Null(beforePreviousWeekly);
            }
        }

        [Fact]
        public void GetCouponWallet()
        {
            IAccountStateDelta states = new MockStateDelta();
            var guid1 = new Guid("6856AE42-A820-4041-92B0-5D7BAA52F2AA");
            var guid2 = new Guid("701BA698-CCB9-4FC7-B88F-7CB8C707D135");
            var guid3 = new Guid("910296E7-34E4-45D7-9B4E-778ED61F278B");
            var coupon1 = new Coupon(guid1, (1, 2));
            var coupon2 = new Coupon(guid2, (1, 2), (3, 4));
            var coupon3 = new Coupon(guid3, (3, 4));
            var agentAddress1 = new Address("0000000000000000000000000000000000000000");
            var agentAddress2 = new Address("0000000000000000000000000000000000000001");

            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty,
                states.GetCouponWallet(agentAddress1));
            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty,
                states.GetCouponWallet(agentAddress2));

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                Bencodex.Types.List.Empty);

            states = states.SetState(
                agentAddress2.Derive(CouponWalletKey),
                Bencodex.Types.Null.Value);

            Assert.Equal(ImmutableDictionary<Guid, Coupon>.Empty, states.GetCouponWallet(agentAddress1));
            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress2));

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                Bencodex.Types.Dictionary.Empty);

            states = states.SetState(
                agentAddress2.Derive(CouponWalletKey),
                (Integer)1);

            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress1));
            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress2));

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                (Bencodex.Types.Boolean)true);

            states = states.SetState(
                agentAddress2.Derive(CouponWalletKey),
                (Text)"test");

            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress1));
            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress2));

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                (Bencodex.Types.Binary)new byte[] { });

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                (Bencodex.Types.Binary)new byte[] { 0x00 });

            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress1));
            Assert.Throws<InvalidCastException>(() => states.GetCouponWallet(agentAddress2));

            states = states.SetState(
                agentAddress1.Derive(CouponWalletKey),
                Bencodex.Types.List.Empty
                    .Add(coupon1.Serialize())
                    .Add(coupon2.Serialize()));

            states = states.SetState(
                agentAddress2.Derive(CouponWalletKey),
                Bencodex.Types.List.Empty
                    .Add(coupon3.Serialize()));

            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(guid1, coupon1)
                    .Add(guid2, coupon2),
                states.GetCouponWallet(agentAddress1));

            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(guid3, coupon3),
                states.GetCouponWallet(agentAddress2));

            Assert.NotEqual(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(guid1, coupon2),
                states.GetCouponWallet(agentAddress2));
        }
    }
}
