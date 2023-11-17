using System;
using System.Collections.Generic;
using UnityEngine;

public delegate string PostCommandFunction(string arg);

public interface AirbridgeWebInterface
{
    string Script { get; }

    void Handle(string message);
}

public class AirbridgeWebInterfaceDefault : AirbridgeWebInterface
{
    public string Script
    {
        get { return ""; }
    }
    
    public void Handle(string message) { }
}

public class AirbridgeWebInterfaceImpl : AirbridgeWebInterface
{
    private string _webToken;
    private PostCommandFunction _postCommandFunction;

    public AirbridgeWebInterfaceImpl(string webToken, PostCommandFunction postCommandFunction)
    {
        _webToken = webToken;
        _postCommandFunction = postCommandFunction;
    }

    public string Script
    {
        get
        {
            int jsonSchemaVersion = 4;
            string sdkVersion = "1.12.1";
            return $@"
AirbridgeNative = {{ }};
AirbridgeNative.postCommand = function (argument) {{
    {_postCommandFunction("argument")}
}};
AirbridgeNative.getWebToken = function () {{
    return ""{_webToken}"";
}};
AirbridgeNative.getJsonSchemaVersion = function () {{
    return {jsonSchemaVersion};
}};
AirbridgeNative.getSdkVersion = function () {{
    return ""{sdkVersion}"";
}};
AirbridgeNative.setUser = function (payload) {{
    AirbridgeNative.postCommand(JSON.stringify({{
        method: ""setUser"",
        payload: payload
    }}));
}};
AirbridgeNative.clearUser = function () {{
    AirbridgeNative.postCommand(JSON.stringify({{
        method: ""clearUser"",
        payload: {{ }}
    }}));
}};
AirbridgeNative.trackEvent = function (payload) {{
    AirbridgeNative.postCommand(JSON.stringify({{
        method: ""trackEvent"",
        payload: payload
    }}));
}};";
        }
    }
    
    public void Handle(string message)
    {
        try
        {
            Dictionary<string, object> obj = (Dictionary<string, object>)AirbridgeJson.Deserialize(message);
            Dictionary<string, object> payload;
            object value;
            switch (obj["method"])
            {
                case "setUser":
                    payload = (Dictionary<string, object>)AirbridgeJson.Deserialize((string)obj["payload"]);
                    AirbridgeUser user = new AirbridgeUser();
                    
                    // Set User ID
                    if (payload.TryGetValue("id", out value) && value != null)
                    {
                        user.SetId((string)value);
                    }
                    // Set User Email
                    if (payload.TryGetValue("email", out value) && value != null)
                    {
                        user.SetEmail((string)value);
                    }
                    // Set User Phone Number
                    if (payload.TryGetValue("phone", out value) && value != null)
                    {
                        user.SetPhoneNumber((string)value);
                    }
                    // Set User Alias
                    if (payload.TryGetValue("alias", out value) && value != null)
                    {
                        Dictionary<string, string> alias = new Dictionary<string, string>();
                        foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)value)
                        {
                            alias[pair.Key] = (string)pair.Value;
                        }
                        user.SetAlias(alias);
                    }
                    // Set User Attributes
                    if (payload.TryGetValue("attributes", out value) && value != null)
                    {
                        user.SetAttributes((Dictionary<string, object>)value);
                    }
                    AirbridgeUnity.SetUser(user);
                    break;
                case "clearUser":
                    AirbridgeUnity.ExpireUser();
                    break;
                case "trackEvent":
                    payload = (Dictionary<string, object>)AirbridgeJson.Deserialize((string)obj["payload"]);
                    if (payload.ContainsKey("category"))
                    {
                        // Set Category
                        AirbridgeEvent @event = new AirbridgeEvent((string)payload["category"]);
                        // Set Action
                        if (payload.TryGetValue("action", out value) && value != null)
                        { 
                            @event.SetAction((string)value);
                        }
                        // Set Label
                        if (payload.TryGetValue("label", out value) && value != null)
                        { 
                            @event.SetLabel((string)value);
                        }
                        // Set Value
                        if (payload.TryGetValue("value", out value) && value != null)
                        { 
                            @event.SetValue(Convert.ToDouble(value));
                        }
                        // Set Custom Attributes
                        if (payload.TryGetValue("custom_attributes", out value) && value != null)
                        { 
                            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)value)
                            {
                                @event.AddCustomAttribute(pair.Key, pair.Value);
                            }
                        }
                        // Set Semantic Attributes
                        if (payload.TryGetValue("semantic_attributes", out value) && value != null)
                        {
                            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)value)
                            {
                                @event.AddSemanticAttribute(pair.Key, pair.Value);
                            }
                        }
                        AirbridgeUnity.TrackEvent(@event);
                    }
                    break;
                default:
                    Debug.Log("[Airbridge][Web Interface] No supported method exists.");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log("[Airbridge][Web Interface] Exception:\n" + e.StackTrace);
        }
    }
}
