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

        private readonly Dictionary<Address, List<Guid>> _agentProducts =
            new Dictionary<Address, List<Guid>>();

        private readonly Dictionary<ItemSubType, List<Guid>> _itemSubTypeProducts =
            new Dictionary<ItemSubType, List<Guid>>(ItemSubTypeComparer.Instance);

        public IReadOnlyDictionary<Guid, ShopItem> Products => _products;

        public IReadOnlyDictionary<Address, List<Guid>> AgentProducts => _agentProducts;

        public IReadOnlyDictionary<ItemSubType, List<Guid>> ItemSubTypeProducts =>
            _itemSubTypeProducts;

        public ShopState() : base(Address)
        {
            PostConstruct();
        }

        public ShopState(Dictionary serialized) : base(serialized)
        {
            _products = ((Dictionary) serialized["products"]).ToDictionary(
                kv => kv.Key.ToGuid(),
                kv => new ShopItem((Dictionary) kv.Value));

            PostConstruct();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "products"] = new Dictionary(
                    _products.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary) kv.Key.Serialize(),
                            kv.Value.Serialize()))),
            }.Union((Dictionary) base.Serialize()));

        private void PostConstruct()
        {
            foreach (var product in _products)
            {
                var productId = product.Value.ProductId;

                var agentAddress = product.Value.SellerAgentAddress;
                if (!_agentProducts.ContainsKey(agentAddress))
                {
                    _agentProducts.Add(agentAddress, new List<Guid>());
                }

                _agentProducts[agentAddress].Add(productId);

                var itemSubType = product.Value.ItemUsable.ItemSubType;
                if (!_itemSubTypeProducts.ContainsKey(itemSubType))
                {
                    _itemSubTypeProducts.Add(itemSubType, new List<Guid>());
                }

                _itemSubTypeProducts[itemSubType].Add(productId);
            }
        }

        #region Register

        public void Register(ShopItem shopItem)
        {
            var sellerAgentAddress = shopItem.SellerAgentAddress;
            var productId = shopItem.ProductId;
            _products[productId] = shopItem;

            if (_agentProducts.ContainsKey(sellerAgentAddress))
            {
                var shopItems = _agentProducts[sellerAgentAddress];
                if (shopItems.Contains(productId))
                {
                    throw new ShopStateAlreadyContainsException(
                        $"{nameof(_agentProducts)}, {sellerAgentAddress}, {productId}");
                }

                shopItems.Add(productId);
            }
            else
            {
                var shopItems = new List<Guid> {productId};
                _agentProducts.Add(sellerAgentAddress, shopItems);
            }

            var itemSubType = shopItem.ItemUsable.ItemSubType;
            if (_itemSubTypeProducts.ContainsKey(itemSubType))
            {
                var shopItems = _itemSubTypeProducts[itemSubType];
                if (shopItems.Contains(productId))
                {
                    throw new ShopStateAlreadyContainsException(
                        $"{nameof(_itemSubTypeProducts)}, {itemSubType}, {productId}");
                }

                shopItems.Add(productId);
            }
            else
            {
                var shopItems = new List<Guid> {productId};
                _itemSubTypeProducts.Add(itemSubType, shopItems);
            }
        }

        #endregion

        #region Unregister

        public void Unregister(Address sellerAgentAddress, ShopItem shopItem)
        {
            Unregister(sellerAgentAddress, shopItem.ProductId);
        }

        public void Unregister(Address sellerAgentAddress, Guid productId)
        {
            if (!TryUnregister(sellerAgentAddress, productId, out _))
            {
                throw new FailedToUnregisterInShopStateException(
                    $"{nameof(_agentProducts)}, {sellerAgentAddress}, {productId}");
            }
        }

        public bool TryUnregister(
            Address sellerAgentAddress,
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

            if (_agentProducts.ContainsKey(sellerAgentAddress))
            {
                var shopItems = _agentProducts[sellerAgentAddress];
                if (shopItems.Any(item => item.Equals(productId)))
                {
                    shopItems.Remove(productId);
                    if (shopItems.Count == 0)
                    {
                        _agentProducts.Remove(sellerAgentAddress);
                    }
                }
            }

            var itemSubType = targetShopItem.ItemUsable.ItemSubType;
            if (_itemSubTypeProducts.ContainsKey(itemSubType))
            {
                var shopItems = _itemSubTypeProducts[itemSubType];
                shopItems.Remove(productId);
                if (shopItems.Count == 0)
                {
                    _itemSubTypeProducts.Remove(itemSubType);
                }
            }

            unregisteredItem = targetShopItem;
            return true;
        }

        #endregion

        public bool TryGet(
            Address sellerAgentAddress,
            Guid productId,
            out ShopItem shopItem)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                shopItem = null;
                return false;
            }

            var shopItems = _agentProducts[sellerAgentAddress];
            if (shopItems.All(item => item != productId))
            {
                shopItem = null;
                return false;
            }

            if (!_products.ContainsKey(productId))
            {
                shopItem = null;
                return false;
            }

            shopItem = _products[productId];
            return true;
        }
    }
}
