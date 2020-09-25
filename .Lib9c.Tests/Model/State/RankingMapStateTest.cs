namespace Lib9c.Tests.Model.State
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RankingMapStateTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _rankingMapAddress;

        public RankingMapStateTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _agentAddress = default(Address);
            _rankingMapAddress = _agentAddress.Derive("ranking_map");
        }

        [Fact]
        public void GetRankingInfos()
        {
            var state = new RankingMapState(_rankingMapAddress);

            for (var i = 0; i < 10; i++)
            {
                var avatarState = new AvatarState(
                    _agentAddress.Derive(i.ToString()),
                    _agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    _rankingMapAddress,
                    "test"
                )
                {
                    exp = 10 - i,
                };
                state.Update(avatarState);
            }

            var list = state.GetRankingInfos(null);
            for (var index = 0; index < list.Count; index++)
            {
                var info = list[index];
                Assert.Equal(10 - index, info.Exp);
                Assert.Equal(_agentAddress.Derive(index.ToString()), info.AvatarAddress);
            }
        }

        [Fact]
        public void Serialize()
        {
            var avatarAddress = _agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                _rankingMapAddress,
                "test"
            );

            var state = new RankingMapState(_rankingMapAddress);
            state.Update(avatarState);
            var serialized = state.Serialize();
            var des = new RankingMapState((Dictionary)serialized);

            Assert.Equal(state.GetRankingInfos(null), des.GetRankingInfos(null));
        }

        [Fact]
        public void SerializeEquals()
        {
            var avatarAddress = _agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                _rankingMapAddress,
                "test"
            );

            var avatarAddress2 = _agentAddress.Derive("avatar2");
            var avatarState2 = new AvatarState(
                avatarAddress2,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                _rankingMapAddress,
                "test2"
            );

            var state = new RankingMapState(_rankingMapAddress);
            state.Update(avatarState);
            state.Update(avatarState2);

            var state2 = new RankingMapState(_rankingMapAddress);
            state2.Update(avatarState2);
            state2.Update(avatarState);

            Assert.Equal(state2.Serialize(), state.Serialize());
        }

        [Fact]
        public void Update()
        {
            var avatarAddress = _agentAddress.Derive("avatar");
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress,
                "test"
            );

            var state = new RankingMapState(rankingMapAddress);
            state.Update(avatarState);

            Assert.Single(state.GetRankingInfos(null));
            Assert.Equal(0, state.GetRankingInfos(null).First().Exp);

            avatarState.exp += 100;
            state.Update(avatarState);
            Assert.Single(state.GetRankingInfos(null));
            Assert.Equal(100, state.GetRankingInfos(null).First().Exp);
        }
    }
}
