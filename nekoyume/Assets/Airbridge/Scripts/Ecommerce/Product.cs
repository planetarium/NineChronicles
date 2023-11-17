using System.Collections;
using System.Collections.Generic;

namespace Airbridge.Ecommerce
{
    public class Product
    {
        private string idKey = "productID";
        private string nameKey = "name";
        private string currencyKey = "currency";
        private string priceKey = "price";
        private string quantityKey = "quantity";
        private string positionKey = "position";

        private Dictionary<string, object> data = new Dictionary<string, object>();

        public void SetId(string id)
        {
            AddData(idKey, id);
        }

        public void SetName(string name)
        {
            AddData(nameKey, name);
        }

        public void SetCurrency(string currency)
        {
            AddData(currencyKey, currency);
        }

        public void SetPrice(double price)
        {
            AddData(priceKey, price);
        }

        public void SetQuantity(int quantity)
        {
            AddData(quantityKey, quantity);
        }

        public void SetPosition(int position)
        {
            AddData(positionKey, position);
        }

        public Dictionary<string, object> ToDictionary()
        {
            return data;
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
}

