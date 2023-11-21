namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CreateAvatar9Test
    {
        private readonly Address _agentAddress;
        private readonly TableSheets _tableSheets;

        public CreateAvatar9Test()
        {
            _agentAddress = default;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Theory]
        [InlineData(0L, 600_000, true, true)]
        [InlineData(7_210_000L, 600_000, true, true)]
        [InlineData(7_210_001L, 200_000, true, true)]
        [InlineData(7_210_001L, 200_000, false, true)]
        [InlineData(7_210_001L, 200_000, true, false)]
        public void Execute(long blockIndex, int expected, bool avatarItemSheetExist, bool avatarFavSheetExist)
        {
            var action = new CreateAvatar9()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var sheets = TableSheetsImporter.ImportSheets();
            var state = new Account(MockState.Empty)
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                );

            foreach (var (key, value) in sheets)
            {
                if (key == nameof(CreateAvatarItemSheet) && !avatarItemSheetExist)
                {
                    continue;
                }

                if (key == nameof(CreateAvatarFavSheet) && !avatarFavSheetExist)
                {
                    continue;
                }

                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            Assert.Equal(0 * CrystalCalculator.CRYSTAL, state.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));

            if (!avatarItemSheetExist && !avatarFavSheetExist)
            {
                var nextState = action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                });

                var avatarAddress = _agentAddress.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CreateAvatar2.DeriveFormat,
                        0
                    )
                );
                Assert.True(nextState.TryGetAgentAvatarStatesV2(
                    default,
                    avatarAddress,
                    out var agentState,
                    out var nextAvatarState,
                    out _)
                );
                Assert.True(agentState.avatarAddresses.Any());
                Assert.Equal("test", nextAvatarState.name);
                Assert.Equal(expected * CrystalCalculator.CRYSTAL, nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
            }
            else
            {
                Assert.Throws<ActionObsoletedException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                }));
            }
        }

        [Theory]
        [InlineData("홍길동")]
        [InlineData("山田太郎")]
        public void ExecuteThrowInvalidNamePatterException(string nickName)
        {
            var agentAddress = default(Address);

            var action = new CreateAvatar9()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = nickName,
            };

            var state = new Account(MockState.Empty);

            Assert.Throws<InvalidNamePatternException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    0
                )
            );

            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var action = new CreateAvatar9()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var state = new Account(MockState.Empty).SetState(avatarAddress, avatarState.Serialize());

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void ExecuteThrowAvatarIndexOutOfRangeException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            var state = new Account(MockState.Empty).SetState(_agentAddress, agentState.Serialize());
            var action = new CreateAvatar9()
            {
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexOutOfRangeException>(() => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteThrowAvatarIndexAlreadyUsedException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    0
                )
            );
            agentState.avatarAddresses[index] = avatarAddress;
            var state = new Account(MockState.Empty).SetState(_agentAddress, agentState.Serialize());

            var action = new CreateAvatar9()
            {
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexAlreadyUsedException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Serialize_With_DotnetAPI()
        {
            var formatter = new BinaryFormatter();
            var action = new CreateAvatar9()
            {
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
            var deserialized = (CreateAvatar9)formatter.Deserialize(ms);

            Assert.Equal(2, deserialized.index);
            Assert.Equal(1, deserialized.hair);
            Assert.Equal(4, deserialized.ear);
            Assert.Equal(5, deserialized.lens);
            Assert.Equal(7, deserialized.tail);
            Assert.Equal("test", deserialized.name);
        }
    }
}
