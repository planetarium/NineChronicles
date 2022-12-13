namespace Nekoyume.Helper
{
    using UniRx;
    public static class LoadingHelper
    {
        public static readonly ReactiveCollection<int> UnlockRuneSlot = new();
        public static readonly ReactiveProperty<bool> RuneEnhancement = new();
    }
}
