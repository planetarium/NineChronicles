using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.UI;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RankingBoardTest
    {
        // FIXME unity test runner not work.
        // private TableSheets _tableSheets;
        //
        // [OneTimeSetUp]
        // public void Init()
        // {
        //     _tableSheets = TableSheetsHelper.MakeTableSheets();
        // }
        //
        // [Test, Sequential]
        // public void GetArenaInfos_By_FirstRank_And_Count(
        //     [Values(100, 100, 10, 10, 10, 10, 0)] int infoCount,
        //     [Values(1, 51, 1, 1, 6, 6, 1)] int firstRank,
        //     [Values(100, 50, 50, 1, 50, 1, 1)] int count,
        //     [Values(100, 50, 10, 1, 5, 1, 0)] int expectedCount)
        // {
        //     var widget = Widget.Create<RankingBoard>();
        //     for (var i = 0; i < infoCount; i++)
        //     {
        //         var avatarState = new AvatarState(
        //             new PrivateKey().ToAddress(),
        //             new PrivateKey().ToAddress(),
        //             0L,
        //             _tableSheets.GetAvatarSheets(),
        //             new GameConfigState(),
        //             default,
        //             i.ToString());
        //         widget.OrderedArenaInfos.Add(new ArenaInfo2(avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, true));
        //     }
        //
        //     var arenaInfos = widget.GetArenaInfos(firstRank, count);
        //     Assert.AreEqual(expectedCount, arenaInfos.Count);
        //
        //     var expectedRank = firstRank;
        //     foreach (var arenaInfo in arenaInfos)
        //     {
        //         Assert.AreEqual(expectedRank++, arenaInfo.rank);
        //     }
        // }
        //
        // [Theory]
        // [InlineData(1, 2)]
        // [InlineData(10, 11)]
        // public void GetArenaInfos_By_FirstRank_And_Count_Throw(int infoCount, int firstRank)
        // {
        //     var weeklyArenaState = new WeeklyArenaState2(new PrivateKey().ToAddress());
        //
        //     for (var i = 0; i < infoCount; i++)
        //     {
        //         var avatarState = new AvatarState(
        //             new PrivateKey().ToAddress(),
        //             new PrivateKey().ToAddress(),
        //             0L,
        //             _tableSheets.GetAvatarSheets(),
        //             new GameConfigState(),
        //             default,
        //             i.ToString());
        //         weeklyArenaState.Update(new ArenaInfo2(avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, true));
        //     }
        //
        //     Assert.Throws<ArgumentOutOfRangeException>(() =>
        //         weeklyArenaState.GetArenaInfos(firstRank, 100));
        // }
        //
        // [Theory]
        // [InlineData(100, 1, 10, 10, 11)]
        // [InlineData(100, 50, 10, 10, 21)]
        // [InlineData(100, 100, 10, 10, 11)]
        // public void GetArenaInfos_By_Upper_And_LowerRange(
        //     int infoCount,
        //     int targetRank,
        //     int upperRange,
        //     int lowerRange,
        //     int expectedCount)
        // {
        //     var weeklyArenaState = new WeeklyArenaState2(new PrivateKey().ToAddress());
        //     Address targetAddress;
        //     for (var i = 0; i < infoCount; i++)
        //     {
        //         var avatarAddress = new PrivateKey().ToAddress();
        //         if (i + 1 == targetRank)
        //         {
        //             targetAddress = avatarAddress;
        //         }
        //
        //         var avatarState = new AvatarState(
        //             avatarAddress,
        //             new PrivateKey().ToAddress(),
        //             0L,
        //             _tableSheets.GetAvatarSheets(),
        //             new GameConfigState(),
        //             default,
        //             i.ToString());
        //         weeklyArenaState.Update(new ArenaInfo2(avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, true));
        //     }
        //
        //     var arenaInfos = weeklyArenaState.GetArenaInfos(targetAddress, upperRange, lowerRange);
        //     Assert.Equal(expectedCount, arenaInfos.Count);
        //
        //     var expectedRank = Math.Max(1, targetRank - upperRange);
        //     foreach (var arenaInfo in arenaInfos)
        //     {
        //         Assert.Equal(expectedRank++, arenaInfo.rank);
        //     }
        // }
    }
}
