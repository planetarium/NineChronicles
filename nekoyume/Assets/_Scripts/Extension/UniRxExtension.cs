using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume
{
    public static class UniRxExtension
    {
        public static void DisposeAll<T>(this ReactiveProperty<T> property) where T : IDisposable
        {
            property.Value?.Dispose();
            property.Dispose();
        }
        
        public static void DisposeAll<T>(this ReactiveProperty<List<T>> property) where T : IDisposable
        {
            property.Value?.DisposeAllAndClear();
            property.Dispose();
        }
        
        public static void DisposeAll<T>(this ReactiveCollection<T> collection) where T : IDisposable
        {
            foreach (var item in collection)
            {
                item.Dispose();
            }
            collection.Dispose();
            collection.Clear();
        }
    }
}
