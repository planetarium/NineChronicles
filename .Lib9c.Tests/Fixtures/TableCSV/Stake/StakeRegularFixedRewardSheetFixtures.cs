namespace Lib9c.Tests.Fixtures.TableCSV.Stake
{
    public static class StakeRegularFixedRewardSheetFixtures
    {
        public const string V1 =
            @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2";

        public const string V2 = @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2
6,5000000,500000,2
7,10000000,500000,2";
    }
}
