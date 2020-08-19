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

        private readonly Dictionary<Address, List<ShopItem>> _agentProducts =
            new Dictionary<Address, List<ShopItem>>();

        private readonly Dictionary<Guid, ShopItem> _products = new Dictionary<Guid, ShopItem>();

        public IReadOnlyDictionary<Address, List<ShopItem>> AgentProducts => _agentProducts;

        public ShopState() : base(Address)
        {
        }

        public ShopState(Dictionary serialized)
            : base(serialized)
        {
            _agentProducts = ((Dictionary) serialized["agentProducts"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => ((List) kv.Value)
                    .Select(d => new ShopItem((Dictionary) d))
                    .ToList()
            );

            _products = ((Dictionary) serialized["products"]).ToDictionary(
                kv => kv.Key.ToGuid(),
                kv => new ShopItem((Dictionary) kv.Value));
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
            }.Union((Dictionary) base.Serialize()));

        #region Register

        public void Register(Address sellerAgentAddress, ShopItem shopItem)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                _agentProducts.Add(sellerAgentAddress, new List<ShopItem>());
            }

            var shopItems = _agentProducts[sellerAgentAddress];
            if (shopItems.Contains(shopItem))
            {
                throw new ShopStateAlreadyContainsException(
                    $"{nameof(_agentProducts)}, {sellerAgentAddress}, {shopItem.ProductId}");
            }

            shopItems.Add(shopItem);
            _agentProducts[sellerAgentAddress] = shopItems;
            _products[shopItem.ProductId] = shopItem;
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
            unregisteredItem = shopItems.FirstOrDefault(item => item.ProductId.Equals(productId));
            if (unregisteredItem is null)
            {
                return false;
            }

            shopItems.Remove(unregisteredItem);
            if (shopItems.Count == 0)
            {
                _agentProducts.Remove(sellerAgentAddress);
            }
            else
            {
                _agentProducts[sellerAgentAddress] = shopItems;
            }

            _products.Remove(unregisteredItem.ProductId);
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
            shopItem = shopItems.FirstOrDefault(item => item.ProductId == productId);
            return !(shopItem is null);
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
