using System;
using System.Collections.Generic;
using System.Linq;

namespace OneStore.Purchasing.Internal
{
    public class OneStorePurchasingInventory
    {
        private readonly object _inventoryLock = new object();
        private readonly Dictionary<string, string> _productIdInventory = new Dictionary<string, string>();
        private readonly Dictionary<string, PurchaseData> _purchaseInventory = new Dictionary<string, PurchaseData>();
        private readonly Dictionary<string, ProductDetail> _productDetailInventory = new Dictionary<string, ProductDetail>();

        /// <summary>
        /// Updates the product id of all products. These products will be alive while the ONE Store instance is alive.
        /// </summary>
        public void UpdateProductIds(IEnumerable<string> productIds)
        {
            lock (_inventoryLock)
            {
                foreach (var id in productIds)
                {
                    _productIdInventory[id] = id;
                }
            }
        }

        /// <summary>
        /// Gets all the product id from inventory.
        /// </summary>
        /// <returns>Returns the product Id list.</returns>
        public List<string> GetAllProductIds()
        {
            lock (_inventoryLock)
            {
                return new List<string>(_productIdInventory.Values);
            }
        }

        /// <summary>
        /// Updates the inventory of productDetails with the provided list of productDetail.
        /// </summary>
        public void UpdateProductDetailInventory(IEnumerable<ProductDetail> productDetailsList)
        {
            lock (_inventoryLock)
            {
                foreach (var productDetail in productDetailsList)
                {
                    _productDetailInventory[productDetail.productId] = productDetail;
                }
            }
        }
        /// <summary>
        /// Updates the inventory of purchases with the provided list of PurchaseData.
        /// </summary>
        public void UpdatePurchaseInventory(IEnumerable<PurchaseData> purchasesList)
        {
            lock (_inventoryLock)
            {
                foreach (var purchase in purchasesList)
                {
                    _purchaseInventory[purchase.ProductId] = purchase;
                }
            }
        }

        /// <summary>
        /// Gets the <cref="ProductDetail"/> associated with the specified ONE Store product id from inventory.
        /// <returns>true if the inventory contains the requested ProductDetail; otherwise, false.</returns>
        /// </summary>
        public bool GetProductDetail(string productId, out ProductDetail productDetail)
        {
            lock (_inventoryLock)
            {
                return _productDetailInventory.TryGetValue(productId, out productDetail);
            }
        }

        /// <summary>
        /// Gets the <cref="PurchaseData"/> associated with the specified ONE Store product id from inventory.
        /// <returns>true if the inventory contains the requested PurchaseData; otherwise, false.</returns>
        /// </summary>
        public bool GetPurchaseData(string productId, out PurchaseData purchase)
        {
            lock (_inventoryLock)
            {
                return _purchaseInventory.TryGetValue(productId, out purchase);
            }
        }

        /// <summary>
        /// Removes the <cref="PurchaseData"/> associated with the specified ONE store productId from inventory.
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        /// </summary>
        public bool RemovePurchase(string productId)
        {
            if (String.IsNullOrEmpty(productId))
            {
                return false;
            }
            
            lock (_inventoryLock)
            {
                var purchaseValue = _purchaseInventory[productId];
                _purchaseInventory.Remove(productId);
                return purchaseValue != null;
            }
        }

        /// <summary>
        /// Gets the list of <cref="ProductDetail"/> associated with the specified ONE Store product Ids from inventory.
        /// <returns>Returns the ProductDetail list.</returns>
        /// </summary>
        public List<ProductDetail> GetProductDetails(IEnumerable<string> productIds)
        {
            lock (_inventoryLock)
            {
                return _productDetailInventory.Values.Where(s => productIds.Count(id => s.productId.Equals(id)) != 0).ToList();
            }
        }

        /// <summary>
        /// Gets all the <see cref="ProductDetail"/> from inventory.
        /// <returns>A dictionary that maps ONE Store product Ids to JSON format strings that represent the
        /// corresponding <see cref="ProductDetail"/>s. </returns>
        /// </summary>
        public Dictionary<string, string> GetAllProductDetails()
        {
            lock (_inventoryLock)
            {
                return _productDetailInventory.ToDictionary(pair => pair.Key, pair => pair.Value.JsonProductDetail);
            }
        }

        public ProductType GetProductType(string productId)
        {
            lock (_inventoryLock)
            {
                ProductDetail productDetail;
                if (GetProductDetail(productId, out productDetail))
                {
                    return ProductType.Get(productDetail.type);
                }

                return null;
            }
        }
    }
}
