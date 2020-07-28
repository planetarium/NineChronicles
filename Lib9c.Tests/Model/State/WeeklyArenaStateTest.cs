namespace Lib9c.Tests.Model.State
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaStateTest : IDisposable
    {
        private TableSheets _tableSheets;

        public WeeklyArenaStateTest()
        {
            _tableSheets = new TableSheets();
            _tableSheets.SetToSheet(nameof(WorldSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestItemRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemRecipeSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemSubRecipeSheet), "test");
            _tableSheets.SetToSheet(
                nameof(CharacterSheet),
                "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed\n100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");
        }

        [Theory]
        [InlineData(1, "44971f56cDDe257b355B7faD618DbD67085e8BB8")]
        [InlineData(2, "866F0C71E0F701cCCCEBAfA17daAbdaB9ee702C1")]
        public void DeriveAddress(int index, string expected)
        {
            var state = new WeeklyArenaState(index);
            Assert.Equal(new Address(expected), state.address);
        }

        public void Dispose()
        {
            _tableSheets = null;
        }

        [Fact]
        public void GetAgentAddresses()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());

            var avatarAddress2 = agentAddress.Derive("avatar2");
            var avatarState2 = new AvatarState(avatarAddress2, agentAddress, 0, _tableSheets, new GameConfigState());

            var state = new WeeklyArenaState(0);
            state.Set(avatarState, _tableSheets.CharacterSheet);
            state[avatarAddress].Activate();
            state.Set(avatarState2, _tableSheets.CharacterSheet);
            state[avatarAddress2].Activate();

            Assert.Single(state.GetAgentAddresses(2));
        }

        [Fact]
        public void Serialize()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new WeeklyArenaState(serialized);

            Assert.Equal(state.address, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (WeeklyArenaState)formatter.Deserialize(ms);
            Assert.Equal(state.address, deserialized.address);
        }
    }
}
