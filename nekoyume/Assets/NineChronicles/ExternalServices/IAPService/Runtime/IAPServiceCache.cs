#nullable enable

using System;
using System.Collections.Generic;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace NineChronicles.ExternalServices.IAPService.Runtime
{
    public class IAPServiceCache
    {
        private class Cache<T>
        {
            private TimeSpan _lifetime;
            private T? _value;

            public TimeSpan Lifetime
            {
                get => _lifetime;
                set
                {
                    if (_lifetime == value)
                    {
                        return;
                    }

                    var from = ExpiresAt - _lifetime;
                    _lifetime = value;
                    ExpiresAt = from + _lifetime;
                }
            }

            public T? Value
            {
                get
                {
                    if (IsExpired)
                    {
                        _value = default;
                    }

                    return _value;
                }
                set
                {
                    _value = value;
                    ExpiresAt = DateTime.UtcNow + _lifetime;
                }
            }

            public DateTime ExpiresAt { get; private set; }

            public bool IsExpired => ExpiresAt < DateTime.UtcNow;

            public Cache(TimeSpan? lifetime = null)
            {
                _lifetime = lifetime ?? TimeSpan.Zero;
                _value = default;
                ExpiresAt = DateTime.MinValue;
            }
        }

        private readonly Cache<ProductSchema[]> _productsCache;

        public ProductSchema[]? Products
        {
            get => _productsCache.Value;
            set => _productsCache.Value = value;
        }

        public readonly Dictionary<string, ReceiptDetailSchema?>
            PurchaseProcessResults;

        public IAPServiceCache(TimeSpan? productsCacheLifetime = null)
        {
            _productsCache = new Cache<ProductSchema[]>(productsCacheLifetime);
            PurchaseProcessResults = new Dictionary<string, ReceiptDetailSchema?>();
        }

        public void SetOptions(TimeSpan? productsCacheLifetime = null)
        {
            _productsCache.Lifetime = productsCacheLifetime ?? TimeSpan.Zero;
        }
    }
}
