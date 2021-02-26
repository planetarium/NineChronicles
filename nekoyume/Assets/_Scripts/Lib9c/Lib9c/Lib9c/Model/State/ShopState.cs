using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// This is a model class of shop state.
    /// </summary>
    [Serializable]
    public class ShopState : State
    {
        public static readonly Address Address = Addresses.Shop;

        private readonly Dictionary<Guid, ShopItem> _products = new Dictionary<Guid, ShopItem>();

        public IReadOnlyDictionary<Guid, ShopItem> Products => _products;

        public ShopState() : base(Address)
        {
        }

        public ShopState(Dictionary serialized) : base(serialized)
        {
            _products = ((Dictionary) serialized["products"]).ToDictionary(
                kv => kv.Key.ToGuid(),
                kv => new ShopItem((Dictionary) kv.Value));
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "products"] = new Dictionary(
                    _products.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary) kv.Key.Serialize(),
                            kv.Value.Serialize()))),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        #region Register

        public void Register(ShopItem shopItem)
        {
            var productId = shopItem.ProductId;
            if (_products.ContainsKey(productId))
            {
                throw new ShopStateAlreadyContainsException($"Aborted as the item already registered # {productId}.");
            }
            _products[productId] = shopItem;
        }

        #endregion

        #region Unregister

        public void Unregister(ShopItem shopItem)
        {
            Unregister(shopItem.ProductId);
        }

        public void Unregister(Guid productId)
        {
            if (!TryUnregister(productId, out _))
            {
                throw new FailedToUnregisterInShopStateException(
                    $"{nameof(_products)}, {productId}");
            }
        }

        public bool TryUnregister(
            Guid productId,
            out ShopItem unregisteredItem)
        {
            if (!_products.ContainsKey(productId))
            {
                unregisteredItem = null;
                return false;
            }

            var targetShopItem = _products[productId];
            _products.Remove(productId);
            unregisteredItem = targetShopItem;
            return true;
        }

        #endregion

        public bool TryGet(
            Address sellerAgentAddress,
            Guid productId,
            out ShopItem shopItem)
        {
            if (!_products.ContainsKey(productId))
            {
                shopItem = null;
                return false;
            }

            shopItem = _products[productId];
            if (shopItem.SellerAgentAddress.Equals(sellerAgentAddress))
            {
                return true;
            }

            shopItem = null;
            return false;
        }
    }
}
