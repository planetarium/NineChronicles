using System;
using Libplanet.Types.Assets;
using Nekoyume.Model.Stake;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class StakingSubject
    {
        private static readonly Subject<StakeStateV2?> _stakeStateV2;
        private static readonly Subject<FungibleAssetValue> _stakedNCG;
        private static readonly Subject<int> _level;
        private static readonly Subject<StakeRegularFixedRewardSheet> _stakeRegularFixedRewardSheet;
        private static readonly Subject<StakeRegularRewardSheet> _stakeRegularRewardSheet;

        public static readonly IObservable<StakeStateV2?> StakeStateV2;
        public static readonly IObservable<FungibleAssetValue> StakedNCG;
        public static readonly IObservable<int> Level;

        public static readonly IObservable<StakeRegularFixedRewardSheet>
            StakeRegularFixedRewardSheet;

        public static readonly IObservable<StakeRegularRewardSheet> StakeRegularRewardSheet;

        static StakingSubject()
        {
            _stakeStateV2 = new Subject<StakeStateV2?>();
            _stakedNCG = new Subject<FungibleAssetValue>();
            _level = new Subject<int>();
            _stakeRegularFixedRewardSheet = new Subject<StakeRegularFixedRewardSheet>();
            _stakeRegularRewardSheet = new Subject<StakeRegularRewardSheet>();

            StakeStateV2 = _stakeStateV2.ObserveOnMainThread();
            StakedNCG = _stakedNCG.ObserveOnMainThread();
            Level = _level.ObserveOnMainThread();
            StakeRegularFixedRewardSheet = _stakeRegularFixedRewardSheet.ObserveOnMainThread();
            StakeRegularRewardSheet = _stakeRegularRewardSheet.ObserveOnMainThread();
        }

        public static void OnNextStakeStateV2(StakeStateV2? stakeStateV2)
        {
            _stakeStateV2.OnNext(stakeStateV2);
        }

        public static void OnNextStakedNCG(FungibleAssetValue stakedNCG)
        {
            _stakedNCG.OnNext(stakedNCG);
        }

        public static void OnNextLevel(int level)
        {
            _level.OnNext(level);
        }

        public static void OnNextStakeRegularFixedRewardSheet(StakeRegularFixedRewardSheet sheet)
        {
            _stakeRegularFixedRewardSheet.OnNext(sheet);
        }

        public static void OnNextStakeRegularRewardSheet(StakeRegularRewardSheet sheet)
        {
            _stakeRegularRewardSheet.OnNext(sheet);
        }
    }
}
