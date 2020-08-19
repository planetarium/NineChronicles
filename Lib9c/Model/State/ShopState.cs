using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// This is a model class of shop state.
    /// </summary>
    [Serializable]
    public class ShopState : State
    {
        public static readonly Address Address = Addresses.Shop;

        private readonly Dictionary<Address, List<Guid>> _agentProducts =
            new Dictionary<Address, List<Guid>>();

        private readonly Dictionary<Guid, ShopItem> _products = new Dictionary<Guid, ShopItem>();

        private readonly Dictionary<ItemSubType, List<Guid>> _itemSubTypeProducts =
            new Dictionary<ItemSubType, List<Guid>>(ItemSubTypeComparer.Instance);

        public IReadOnlyDictionary<Address, List<Guid>> AgentProducts => _agentProducts;

        public IReadOnlyDictionary<Guid, ShopItem> Products => _products;

        public IReadOnlyDictionary<ItemSubType, List<Guid>> ItemSubTypeProducts => _itemSubTypeProducts;

        public ShopState() : base(Address)
        {
        }

        public ShopState(Dictionary serialized) : base(serialized)
        {
            _agentProducts = ((Dictionary) serialized["agentProducts"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => ((List) kv.Value)
                    .Select(d => d.ToGuid())
                    .ToList());

            _products = ((Dictionary) serialized["products"]).ToDictionary(
                kv => kv.Key.ToGuid(),
                kv => new ShopItem((Dictionary) kv.Value));

            _itemSubTypeProducts = ((Dictionary) serialized["itemSubTypeProducts"]).ToDictionary(
                kv => kv.Key.ToEnum<ItemSubType>(),
                kv => ((List) kv.Value)
                    .Select(value => value.ToGuid())
                    .ToList());
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "agentProducts"] = new Dictionary(
                    _agentProducts.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary) kv.Key.Serialize(),
                            new List(kv.Value.Select(i => i.Serialize()))))),
                [(Text) "products"] = new Dictionary(
                    _products.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary) kv.Key.Serialize(),
                            kv.Value.Serialize()))),
                [(Text) "itemSubTypeProducts"] = new Dictionary(
                    _itemSubTypeProducts.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Text) kv.Key.Serialize(),
                            new List(kv.Value.Select(i => i.Serialize()))))),
            }.Union((Dictionary) base.Serialize()));

        #region Register

        public void Register(Address sellerAgentAddress, ShopItem shopItem)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                _agentProducts.Add(sellerAgentAddress, new List<Guid>());
            }

            var shopItems = _agentProducts[sellerAgentAddress];
            if (shopItems.Contains(shopItem.ProductId))
            {
                throw new ShopStateAlreadyContainsException(
                    $"{nameof(_agentProducts)}, {sellerAgentAddress}, {shopItem.ProductId}");
            }

            shopItems.Add(shopItem.ProductId);
            _agentProducts[sellerAgentAddress] = shopItems;
            _products[shopItem.ProductId] = shopItem;

            if (!_itemSubTypeProducts.ContainsKey(shopItem.ItemUsable.ItemSubType))
            {
                _itemSubTypeProducts.Add(shopItem.ItemUsable.ItemSubType, new List<Guid>());
            }

            _itemSubTypeProducts[shopItem.ItemUsable.ItemSubType].Add(shopItem.ProductId);
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
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                unregisteredItem = null;
                return false;
            }

            var shopItems = _agentProducts[sellerAgentAddress];

            if (!shopItems.Any(item => item.Equals(productId)))
            {
                unregisteredItem = null;
                return false;
            }

            shopItems.Remove(productId);
            if (shopItems.Count == 0)
            {
                _agentProducts.Remove(sellerAgentAddress);
            }
            else
            {
                _agentProducts[sellerAgentAddress] = shopItems;
            }

            if (!_products.ContainsKey(productId))
            {
                unregisteredItem = null;
                return false;
            }

            unregisteredItem = _products[productId];
            _products.Remove(productId);

            var itemSubType = unregisteredItem.ItemUsable.ItemSubType;
            if (_itemSubTypeProducts.ContainsKey(itemSubType))
            {
                var guids = _itemSubTypeProducts[itemSubType];
                guids.Remove(productId);
                if (guids.Count == 0)
                {
                    _itemSubTypeProducts.Remove(itemSubType);
                }
                else
                {
                    _itemSubTypeProducts[itemSubType] = guids;
                }
            }

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

    [Serializable]
    public class ShopStateAlreadyContainsException : Exception
    {
        public ShopStateAlreadyContainsException(string message) : base(message)
        {
        }

        protected ShopStateAlreadyContainsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class FailedToUnregisterInShopStateException : Exception
    {
        public FailedToUnregisterInShopStateException(string message) : base(message)
        {
        }

        protected FailedToUnregisterInShopStateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
