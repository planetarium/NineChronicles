using System;
using Libplanet.Assets;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class MonsterCollectionStateSubject
    {
        private static readonly Subject<int> _level;

        public static readonly IObservable<int> Level;

        static MonsterCollectionStateSubject()
        {
            _level = new Subject<int>();
            Level = _level.ObserveOnMainThread();
        }

        public static void OnNextLevel(int level)
        {
            _level.OnNext(level);
        }
    }
}
