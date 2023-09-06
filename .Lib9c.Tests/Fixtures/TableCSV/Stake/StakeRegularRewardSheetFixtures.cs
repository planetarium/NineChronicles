namespace Lib9c.Tests.Fixtures.TableCSV.Stake
{
    public static class StakeRegularRewardSheetFixtures
    {
        public const string V1 =
            @"level,required_gold,item_id,rate,type,currency_ticker,currency_decimal_places,decimal_rate
1,50,400000,,Item,,,10
1,50,500000,,Item,,,800
1,50,20001,,Rune,,,6000
2,500,400000,,Item,,,8
2,500,500000,,Item,,,800
2,500,20001,,Rune,,,6000
3,5000,400000,,Item,,,5
3,5000,500000,,Item,,,800
3,5000,20001,,Rune,,,6000
4,50000,400000,,Item,,,5
4,50000,500000,,Item,,,800
4,50000,20001,,Rune,,,6000
5,500000,400000,,Item,,,5
5,500000,500000,,Item,,,800
5,500000,20001,,Rune,,,6000";

        public const string V2 =
            @"level,required_gold,item_id,rate,type,currency_ticker
1,50,400000,10,Item,
1,50,500000,800,Item,
1,50,20001,6000,Rune,
2,500,400000,4,Item,
2,500,500000,600,Item,
2,500,20001,6000,Rune,
3,5000,400000,2,Item,
3,5000,500000,400,Item,
3,5000,20001,6000,Rune,
4,50000,400000,2,Item,
4,50000,500000,400,Item,
4,50000,20001,6000,Rune,
5,500000,400000,2,Item,
5,500000,500000,400,Item,
5,500000,20001,6000,Rune,
6,5000000,400000,2,Item,
6,5000000,500000,400,Item,
6,5000000,20001,6000,Rune,
6,5000000,800201,50,Item,
7,10000000,400000,2,Item,
7,10000000,500000,400,Item,
7,10000000,20001,6000,Rune,
7,10000000,600201,50,Item,
7,10000000,800201,50,Item,
7,10000000,,100,Currency,GARAGE";
        // NOTE: belows are same.
        // since "claim_stake_reward8".
        // 7,10000000,20001,6000,Rune,
        // 7,10000000,,6000,Rune,
        // since "claim_stake_reward9".
        // 7,10000000,,6000,Currency,RUNE_GOLDENLEAF
    }
}
