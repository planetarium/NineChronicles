using System;
using UniRx;
using Uno.Extensions;

namespace Nekoyume
{
    public static class UniRxExtension
    {
        public static void DisposeAll<T>(this ReactiveCollection<T> collection) where T : IDisposable
        {
            collection.Dispose();
            collection.ForEach(obj => obj.Dispose());
        }
    }
}
