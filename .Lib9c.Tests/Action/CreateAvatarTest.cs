namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CreateAvatarTest
    {
        [Fact]
        public void Execute()
        {
            var agentAddress = default(Address);
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new CreateAvatar()
            {
                avatarAddress = avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var sheets = TableSheetsImporter.ImportSheets();
            var state = new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(
                    Addresses.GoldDistribution,
                    GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize()
                )
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                )
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 0,
            });

            Assert.Equal(
                CreateAvatar.InitialGoldBalance,
                nextState.GetBalance(default, gold.Currency).MajorUnit
            );
            Assert.True(nextState.TryGetAgentAvatarStates(
                default,
                avatarAddress,
                out var agentState,
                out var nextAvatarState)
            );
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
        }

        [Fact]
        public void Rehearsal()
        {
            var agentAddress = default(Address);
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new CreateAvatar()
            {
                avatarAddress = avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            var updatedAddresses = new List<Address>()
            {
                agentAddress,
                avatarAddress,
                Addresses.GoldCurrency,
            };
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                var slotAddress = avatarAddress.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                updatedAddresses.Add(slotAddress);
            }

            var state = new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize());

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(
                updatedAddresses.ToImmutableHashSet(),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var formatter = new BinaryFormatter();
            var action = new CreateAvatar()
            {
                avatarAddress = default,
                index = 2,
                hair = 1,
                ear = 4,
                lens = 5,
                tail = 7,
                name = "test",
            };

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (CreateAvatar)formatter.Deserialize(ms);

            Assert.Equal(default, deserialized.avatarAddress);
            Assert.Equal(2, deserialized.index);
            Assert.Equal(1, deserialized.hair);
            Assert.Equal(4, deserialized.ear);
            Assert.Equal(5, deserialized.lens);
            Assert.Equal(7, deserialized.tail);
            Assert.Equal("test", deserialized.name);
        }
    }
}
