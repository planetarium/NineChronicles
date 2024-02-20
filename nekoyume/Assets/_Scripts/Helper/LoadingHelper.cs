using System;

namespace Nekoyume.Helper
{
    using UniRx;
    public static class LoadingHelper
    {
        public static readonly ReactiveCollection<int> UnlockRuneSlot = new();
        public static readonly ReactiveProperty<bool> RuneEnhancement = new();
        public static readonly ReactiveProperty<int> PetEnhancement = new(0);
        public static readonly ReactiveProperty<Tuple<int, int>> Summon = new();
        public static readonly ReactiveProperty<bool> ClaimStakeReward = new();
        public static readonly ReactiveProperty<int> ActivateCollection = new(0);
    }
}
