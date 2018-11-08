using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Action;
using Planetarium.SDK.Address;
using Planetarium.SDK.Tx;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nekoyume.Move
{
    internal class Name : Attribute
    {
        public string Value {get; private set;}

        public Name(string value)
        {
            Value = value;
        }
    }
    public class Move : BaseTransaction, IAction
    {
        public byte[] UserAddress
        {
            get
            {
                return PublicKey.ToAddress();
            }
        }
        public Dictionary<string, string> Details { get; internal set; }
        public string Name 
        { 
            get 
            {
                foreach(var attr in GetType().GetCustomAttributes()) 
                {
                    if (attr is Name) 
                    {
                        return (attr as Name).Value;
                    }
                }
                return null;
            }
        }
        public int Tax { get; internal set; }
        public new DateTime Timestamp { get; internal set; }

        public override IDictionary<string, dynamic> PlainValue
        {
            get
            {
                return new Dictionary<string, dynamic>
                {
                    { "user_address", UserAddress.Hex() },
                    { "name", Name },
                    { "details", Details },
                    { "created_at", Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff") },
                    { "tax", Tax }
                };
            }
        }

        public bool Valid
        {
            get
            {
                if (Signature == null)
                {
                    return false;
                }

                if (!PublicKey.Verify(Serialize(false), Signature))
                {
                    return false;
                }

                return true;
            }
        }

        public new void Sign(PrivateKey privateKey)
        {
            base.Sign(privateKey);
            PublicKey = privateKey.PublicKey;
        }
    }

    [Name("hack_and_slash")]
    public class HackAndSlash : Move
    {
    }

    [Name("sleep")]
    public class Sleep : Move
    {
    }

    [Name("create_novice")]
    public class CreateNovice : Move
    {
    }

    [Name("first_class")]
    public class FirstClass : Move
    {
    }

    [Name("move_zone")]
    public class MoveZone : Move
    {
    }

    [Name("level_up")]
    public class LevelUp : Move
    {
    }

    [Name("say")]
    public class Say : Move
    {
    }

    [Name("send")]
    public class Send : Move
    {
    }

    [Name("sell")]
    public class Sell : Move
    {
    }

    [Name("buy")]
    public class Buy : Move
    {
    }

    [Name("combine")]
    public class Combine: Move
    {
    }
}
