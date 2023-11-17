using System.Collections.Generic;

public class AirbridgeEvent
{
    private const string categoryKey = "category";
    private const string actionKey = "action";
    private const string labelKey = "label";
    private const string valueKey = "value";

    private const string semanticAttributesKey = "semanticAttributes";
    private const string queryKey = "query";
    private const string cartIdKey = "cartID";
    private const string productListIdKey = "productListID";
    private const string transactionIdKey = "transactionID";
    private const string inAppPurchasedKey = "inAppPurchased";
    private const string currencyKey = "currency";
    private const string totalValueKey = "totalValue";
    private const string totalQuantityKey = "totalQuantity";
    private const string productsKey = "products";

    private const string customAttributesKey = "customAttributes";

    private Dictionary<string, object> data = new Dictionary<string, object>();
    private Dictionary<string, object> semanticAttributes = new Dictionary<string, object>();
    private Dictionary<string, object> customAttributes = new Dictionary<string, object>();

    public AirbridgeEvent(string category)
    {
        AddData(categoryKey, category);
        AddData(semanticAttributesKey, semanticAttributes);
        AddData(customAttributesKey, customAttributes);
    }

    #region default attributes

    public void SetAction(string action)
    {
        AddData(actionKey, action);
    }

    public void SetLabel(string label)
    {
        AddData(labelKey, label);
    }

    public void SetValue(double value)
    {
        AddData(valueKey, value);
    }

    #endregion

    #region semantic attributes

    public void SetQuery(string query)
    {
        AddSemanticAttribute(queryKey, query);
    }

    public void SetCartId(string cartId)
    {
        AddSemanticAttribute(cartIdKey, cartId);
    }

    public void SetTransactionId(string transactionId)
    {
        AddSemanticAttribute(transactionIdKey, transactionId);
    }

    public void SetProducts(params Airbridge.Ecommerce.Product[] products)
    {
        List<Dictionary<string, object>> serialized = new List<Dictionary<string, object>>();
        foreach (Airbridge.Ecommerce.Product product in products)
        {
            serialized.Add(product.ToDictionary());
        }
        AddSemanticAttribute(productsKey, serialized);
    }

    public void SetProductListId(string productListId)
    {
        AddSemanticAttribute(productListIdKey, productListId);
    }

    public void SetInAppPurchased(bool inAppPurchased)
    {
        AddSemanticAttribute(inAppPurchasedKey, inAppPurchased);
    }

    public void SetCurrency(string currency)
    {
        AddSemanticAttribute(currencyKey, currency);
    }

    public void SetTotalValue(double totalValue)
    {
        AddSemanticAttribute(totalValueKey, totalValue);
    }

    public void SetTotalQuantity(int totalQuantity)
    {
        AddSemanticAttribute(totalQuantityKey, totalQuantity);
    }

    public void AddSemanticAttribute(string key, object value)
    {
        if (!semanticAttributes.ContainsKey(key))
        {
            semanticAttributes.Add(key, value);
        }
        else
        {
            semanticAttributes[key] = value;
        }
    }

    #endregion

    #region custom attributes

    public void AddCustomAttribute(string key, object value)
    {
        if (customAttributes.ContainsKey(key))
        {
            customAttributes[key] = value;
        }
        else
        {
            customAttributes.Add(key, value);
        }
    }

    #endregion

    public string ToJsonString()
    {
        return AirbridgeJson.Serialize(data);
    }

    private void AddData(string key, object value)
    {
        if (!data.ContainsKey(key))
        {
            data.Add(key, value);
        }
        else
        {
            data[key] = value;
        }
    }
}
