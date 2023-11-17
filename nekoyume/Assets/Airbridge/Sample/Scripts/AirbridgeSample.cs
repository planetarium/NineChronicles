using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirbridgeSample : MonoBehaviour
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string UserAliasKey { get; set; }
    public string UserAliasValue { get; set; }
    public string UserAttributeKey { get; set; }
    public string UserAttributeValue { get; set; }

    public int ProductIndex { get; set; }

    public string DeviceKey { get; set; }
    public string DeviceValue { get; set; }

    private List<Airbridge.Ecommerce.Product> beverages = new List<Airbridge.Ecommerce.Product>();

    public void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        AppTrackingTransparency.OnAuthorizationStatusReceived += OnAuthorizationStatusReceived;
        AppTrackingTransparency.AuthorizationStatus status = AppTrackingTransparency.TrackingAuthorizationStatus();
        if (status == AppTrackingTransparency.AuthorizationStatus.NotDetermined)
        {
            AppTrackingTransparency.RequestTrackingAuthorization();
        }
        else
        {
            Init();
        }
#else
        Init();
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    void OnAuthorizationStatusReceived(AppTrackingTransparency.AuthorizationStatus status)
    {
        AppTrackingTransparency.OnAuthorizationStatusReceived -= OnAuthorizationStatusReceived;
        Init();
    }
#endif

    private void Init()
    {
        AirbridgeUnity.SetDeeplinkCallback("AirbridgeSample");
        AirbridgeUnity.SetOnAttributionReceived("AirbridgeSample");

        // Create beverages
        Airbridge.Ecommerce.Product cocacola = new Airbridge.Ecommerce.Product();
        cocacola.SetId("beverage_1");
        cocacola.SetName("Coca Cola");
        cocacola.SetPrice(1.25f);
        cocacola.SetCurrency("USD");
        cocacola.SetQuantity(1);
        cocacola.SetPosition(0);
        beverages.Add(cocacola);

        Airbridge.Ecommerce.Product cocacolaPack = new Airbridge.Ecommerce.Product();
        cocacolaPack.SetId("beverage_2");
        cocacolaPack.SetName("Coca Cola 12 Pack");
        cocacolaPack.SetPrice(12.99f);
        cocacolaPack.SetCurrency("USD");
        cocacolaPack.SetQuantity(1);
        cocacolaPack.SetPosition(1);
        beverages.Add(cocacolaPack);

        Airbridge.Ecommerce.Product fanta = new Airbridge.Ecommerce.Product();
        fanta.SetId("beverage_3");
        fanta.SetName("Fanta");
        fanta.SetPrice(1500f);
        fanta.SetCurrency("KRW");
        fanta.SetQuantity(1);
        fanta.SetPosition(2);
        beverages.Add(fanta);

        Airbridge.Ecommerce.Product fantaWithCocacola = new Airbridge.Ecommerce.Product();
        fantaWithCocacola.SetId("beverage_4");
        fantaWithCocacola.SetName("Fanta & Coca cola");
        fantaWithCocacola.SetPrice(2500f);
        fantaWithCocacola.SetCurrency("KRW");
        fantaWithCocacola.SetQuantity(1);
        fantaWithCocacola.SetPosition(3);
        beverages.Add(fantaWithCocacola);
    }

    // Method will call by Airbridge when deeplink detected
    private void OnTrackingLinkResponse(string url)
    {
        ToastMessage.Show(url);
    }

    // Method will call by Airbridge when attribution result received
    void OnAttributionResultReceived(string jsonString)
    {
        ToastMessage.Show(jsonString);
    }

    public void ApplyUserClick()
    {
        AirbridgeUser user = new AirbridgeUser();
        if (!string.IsNullOrEmpty(UserId))
        {
            user.SetId(UserId);
        }
        if (!string.IsNullOrEmpty(Email))
        {
            user.SetEmail(Email);
        }
        if (!string.IsNullOrEmpty(Phone))
        {
            user.SetPhoneNumber(Phone);
        }

        user.SetAlias("alias1", "value1");
        Dictionary<string, object> attrs = new Dictionary<string, object>();
        attrs["int"] = 123;
        attrs["long"] = 1234L;
        attrs["float"] = 123.456f;
        attrs["bool"] = true;
        attrs["string"] = "text";
        user.SetAttributes(attrs);

        AirbridgeUnity.SetUser(user);
    }

    public void ExpireUser()
    {
        AirbridgeUnity.ExpireUser();
    }

    public void SignUpClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.SIGN_UP);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void SignInClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.SIGN_IN);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void SignOutClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.SIGN_OUT);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void ViewHomeClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.VIEW_HOME);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void ViewSearchResultClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.VIEW_SEARCH_RESULT);
        @event.SetQuery("SELECT * FROM beverages");
        @event.SetProducts(beverages.ToArray());
        AirbridgeUnity.TrackEvent(@event);
    }

    public void ViewProductListClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.VIEW_PRODUCT_LIST);
        @event.SetProductListId("beverage_list_0");
        @event.SetProducts(beverages.ToArray());
        AirbridgeUnity.TrackEvent(@event);
    }

    public void ViewProductDetailsClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.VIEW_PRODUCT_DETAILS);
        @event.SetProducts(beverages[ProductIndex]);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void AddToCartClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.ADD_TO_CART);
        @event.SetCartId("cart_1");
        @event.SetProducts(beverages[ProductIndex]);
        @event.SetCurrency("usd");
        @event.SetTotalValue(123.456f);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void OrderClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent(Airbridge.Constants.CATEGORY.ORDER_COMPLETE);
        @event.SetTransactionId("transaction_123");
        @event.SetProducts(beverages[ProductIndex]);
        @event.SetInAppPurchased(true);
        @event.SetCurrency("usd");
        @event.SetTotalValue(123.456f);
        AirbridgeUnity.TrackEvent(@event);
    }

    public void CustomEventClick()
    {
        AirbridgeEvent @event = new AirbridgeEvent("custom category");
        @event.SetAction("custom action");
        @event.SetLabel("custom label");
        @event.SetValue(12345);
        @event.AddCustomAttribute("custom_int", 123);
        @event.AddCustomAttribute("custom_string", "text123");
        @event.AddCustomAttribute("custom_double", 123.456);
        Dictionary<string, string> customMap = new Dictionary<string, string>();
        customMap.Add("map_key", "test123");
        @event.AddCustomAttribute("custom_map", customMap);
        List<string> customList = new List<string>();
        customList.Add("value1");
        customList.Add("value2");
        customList.Add("value3");
        @event.AddCustomAttribute("custom_list", customList);

        @event.AddSemanticAttribute("query", "query123");
        @event.AddSemanticAttribute("currency", "usd");
        @event.AddSemanticAttribute("totalQuantity", 10);
        @event.AddSemanticAttribute("totalValue", 100);
        @event.AddSemanticAttribute("cartID", "cart_1234");
        AirbridgeUnity.TrackEvent(@event);
    }

    public void SetDeviceAlias()
    {
        if (!string.IsNullOrEmpty(DeviceKey) && !string.IsNullOrEmpty(DeviceValue))
        {
            AirbridgeUnity.SetDeviceAlias(DeviceKey, DeviceValue);
        }
    }

    public void RemoveDeviceAlias()
    {
        if (!string.IsNullOrEmpty(DeviceKey))
        {
            AirbridgeUnity.RemoveDeviceAlias(DeviceKey);
        }
    }

    public void ClearDeviceAlias()
    {
        AirbridgeUnity.ClearDeviceAlias();
    }
}
