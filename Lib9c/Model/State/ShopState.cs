using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public ShopItem Register(Address sellerAgentAddress, ShopItem shopItem)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                _agentProducts.Add(sellerAgentAddress, new List<ShopItem>());
            }

            _agentProducts[sellerAgentAddress].Add(shopItem);
            return shopItem;
        }

        public bool Unregister(Address sellerAgentAddress, ShopItem shopItem)
        {
            return Unregister(sellerAgentAddress, shopItem.ProductId);
        }

        public bool Unregister(Address sellerAgentAddress, Guid productId)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
                return false;

            var shopItems = _agentProducts[sellerAgentAddress];
            var shopItem = shopItems.FirstOrDefault(item => item.ProductId.Equals(productId));
            if (shopItem is null)
                return false;

            shopItems.Remove(shopItem);
            if (shopItems.Count == 0)
            {
                _agentProducts.Remove(sellerAgentAddress);
            }

            return true;
        }

        public bool TryGet(Address sellerAgentAddress, Guid productId,
            out KeyValuePair<Address, ShopItem> outPair)
        {
            if (!_agentProducts.ContainsKey(sellerAgentAddress))
            {
                return false;
            }

            var list = _agentProducts[sellerAgentAddress];

            foreach (var shopItem in list)
            {
                if (shopItem.ProductId != productId)
                {
                    continue;
                }

                outPair = new KeyValuePair<Address, ShopItem>(sellerAgentAddress, shopItem);
                return true;
            }

            return false;
        }

        public bool TryUnregister(Address sellerAgentAddress,
            Guid productId, out ShopItem outUnregisteredItem)
        {
            if (!TryGet(sellerAgentAddress, productId, out var outPair))
            {
                outUnregisteredItem = null;
                return false;
            }

            _agentProducts[outPair.Key].Remove(outPair.Value);

            outUnregisteredItem = outPair.Value;
            return true;
        }
    }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "agentProducts"] = new Dictionary(
                    _agentProducts.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary) kv.Key.Serialize(),
                            new List(kv.Value.Select(i => i.Serialize()))
                        )
                    )
                )
            }.Union((Dictionary) base.Serialize()));
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
    public class NotFoundInShopStateException : Exception
    {
        public NotFoundInShopStateException(string message) : base(message)
        {
        }

        protected NotFoundInShopStateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
