using System;
using Libplanet.Types.Assets;
using Nekoyume.Model.Stake;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class StakingSubject
    {
        private static readonly Subject<StakeState?> StakeStateV2Internal;
        private static readonly Subject<FungibleAssetValue> StakedNCGInternal;
        private static readonly Subject<int> LevelInternal;
        private static readonly Subject<StakeRegularFixedRewardSheet> StakeRegularFixedRewardSheetInternal;
        private static readonly Subject<StakeRegularRewardSheet> StakeRegularRewardSheetInternal;

        public static readonly IObservable<StakeState?> StakeStateV2;
        public static readonly IObservable<FungibleAssetValue> StakedNCG;
        public static readonly IObservable<int> Level;
        public static readonly IObservable<StakeRegularFixedRewardSheet>
            StakeRegularFixedRewardSheet;
        public static readonly IObservable<StakeRegularRewardSheet> StakeRegularRewardSheet;

        static StakingSubject()
        {
            StakeStateV2Internal = new Subject<StakeState?>();
            StakedNCGInternal = new Subject<FungibleAssetValue>();
            LevelInternal = new Subject<int>();
            StakeRegularFixedRewardSheetInternal = new Subject<StakeRegularFixedRewardSheet>();
            StakeRegularRewardSheetInternal = new Subject<StakeRegularRewardSheet>();

            StakeStateV2 = StakeStateV2Internal.ObserveOnMainThread();
            StakedNCG = StakedNCGInternal.ObserveOnMainThread();
            Level = LevelInternal.ObserveOnMainThread();
            StakeRegularFixedRewardSheet = StakeRegularFixedRewardSheetInternal.ObserveOnMainThread();
            StakeRegularRewardSheet = StakeRegularRewardSheetInternal.ObserveOnMainThread();
        }

        public static void OnNextStakeStateV2(StakeState? stakeStateV2)
        {
            StakeStateV2Internal.OnNext(stakeStateV2);
        }

        public static void OnNextStakedNCG(FungibleAssetValue stakedNCG)
        {
            StakedNCGInternal.OnNext(stakedNCG);
        }

        public static void OnNextLevel(int level)
        {
            LevelInternal.OnNext(level);
        }

        public static void OnNextStakeRegularFixedRewardSheet(StakeRegularFixedRewardSheet sheet)
        {
            StakeRegularFixedRewardSheetInternal.OnNext(sheet);
        }

        public static void OnNextStakeRegularRewardSheet(StakeRegularRewardSheet sheet)
        {
            StakeRegularRewardSheetInternal.OnNext(sheet);
        }
    }
}
