using Nekoyume.Model;
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
        public string Value { get; private set; }

        public Name(string value)
        {
            Value = value;
        }
    }
    public abstract class Move : BaseTransaction, IAction
    {
        public byte[] UserAddress
        {
            get
            {
                return PublicKey.ToAddress();
            }
        }
        public Dictionary<string, string> Details { get; set; }
        public string Name
        {
            get
            {
                foreach (var attr in GetType().GetCustomAttributes())
                {
                    if (attr is Name)
                    {
                        return (attr as Name).Value;
                    }
                }
                return null;
            }
        }
        public int Tax { get; set; }
        public new DateTime Timestamp { get; set; }

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
            PublicKey = privateKey.PublicKey;
            base.Sign(privateKey);
        }

        public abstract Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar);
    }

    [Name("hack_and_slash")]
    public class HackAndSlash : Move
    {
        public HackAndSlash(Dictionary<string, string> details)
        {
            Details = details;
        }

        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            if (avatar == null)
            {
                // TODO require Moves
                avatar = Avatar.Get(UserAddress, null);
            }
            if (avatar.dead)
            {
                // TODO Implement InvalidMoveException
                throw new Exception();
            }
            throw new NotImplementedException();
        }
    }

    [Name("sleep")]
    public class Sleep : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            throw new NotImplementedException();
        }
    }

    [Name("create_novice")]
    public class CreateNovice : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            Dictionary<string, string> result = new Dictionary<string, string>
            {
                {"type", "create_novice"},
                {"result", "success"}
            };
            return new Tuple<Avatar, Dictionary<string, string>>(
                new Avatar
                {
                    name = Details["name"],
                    user = UserAddress,
                    gold = 0,
                    class_ = "novice",
                    level = 1,
                    zone = "zone_0",
                    strength = 10,
                    dexterity = 8,
                    intelligence = 9,
                    constitution = 9,
                    luck = 9
                }, result
            );
        }
    }

    [Name("first_class")]
    public class FirstClass : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            throw new NotImplementedException();
        }
    }

    [Name("move_zone")]
    public class MoveZone : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            throw new NotImplementedException();
        }
    }
}
