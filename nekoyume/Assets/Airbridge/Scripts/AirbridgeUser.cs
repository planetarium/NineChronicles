using System.Collections.Generic;
using UnityEngine;

public class AirbridgeUser
{
    private string id;
    private string email;
    private string phoneNumber;
    private Dictionary<string, string> alias = new Dictionary<string, string>();
    private Dictionary<string, object> attrs = new Dictionary<string, object>();

    public AirbridgeUser(string id = null, string email = null, string phoneNumber = null)
    {
        this.id = id;
        this.email = email;
        this.phoneNumber = phoneNumber;
    }

    public string GetId()
    {
        return id;
    }

    public void SetId(string id)
    {
        this.id = id;
    }

    public string GetEmail()
    {
        return email;
    }

    public void SetEmail(string email)
    {
        this.email = email;
    }

    public string GetPhoneNumber()
    {
        return phoneNumber;
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        this.phoneNumber = phoneNumber;
    }

    public Dictionary<string, string> GetAlias()
    {
        return alias;
    }

    public void SetAlias(string key, string value)
    {
        if (!alias.ContainsKey(key))
        {
            alias.Add(key, value);
        }
        else
        {
            alias[key] = value;
        }
    }

    public void SetAlias(Dictionary<string, string> alias)
    {
        this.alias.Clear();
        foreach (var pair in alias)
        {
            SetAlias(pair.Key, pair.Value);
        }
    }

    public Dictionary<string, object> GetAttributes()
    {
        return attrs;
    }

    public void SetAttribute(string key, int value)
    {
        attrs[key] = value;
    }

    public void SetAttribute(string key, long value)
    {
        attrs[key] = value;
    }

    public void SetAttribute(string key, float value)
    {
        attrs[key] = value;
    }

    public void SetAttribute(string key, bool value)
    {
        attrs[key] = value;
    }

    public void SetAttribute(string key, string value)
    {
        attrs[key] = value;
    }

    public void SetAttributes(Dictionary<string, object> attrs)
    {
        this.attrs.Clear();
        foreach (string key in attrs.Keys)
        {
            object value = attrs[key];
            if (value is int)
            {
                SetAttribute(key, (int)value);
            }
            else if (value is long)
            {
                SetAttribute(key, (long)value);
            }
            else if (value is float)
            {
                SetAttribute(key, (float)value);
            }
            else if (value is bool)
            {
                SetAttribute(key, (bool)value);
            }
            else if (value is string)
            {
                SetAttribute(key, (string)value);
            }
            else
            {
                Debug.LogWarning("Invalid 'user-attribute' value data type received. The value will ignored");
            }
        }
    }

    public class Builder
    {
        private AirbridgeUser result = new AirbridgeUser();

        public Builder SetId(string id)
        {
            result.SetId(id);
            return this;
        }

        public Builder SetEmail(string email)
        {
            result.SetEmail(email);
            return this;
        }

        public Builder SetPhoneNumber(string phoneNumber)
        {
            result.SetPhoneNumber(phoneNumber);
            return this;
        }

        public Builder SetAlias(string key, string value)
        {
            result.SetAlias(key, value);
            return this;
        }

        public Builder SetUserAlias(Dictionary<string, string> alias)
        {
            result.SetAlias(alias);
            return this;
        }

        public AirbridgeUser build()
        {
            AirbridgeUser copy = result;
            this.result = null;
            return copy;
        }
    }
}
