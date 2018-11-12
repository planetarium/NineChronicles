using Nekoyume.Model;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Action;
using Planetarium.SDK.Address;
using Planetarium.SDK.Tx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Nekoyume.Move
{
    internal class MoveName : Attribute
    {
        public string Value { get; private set; }

        public MoveName(string value)
        {
            Value = value;
        }

        public static string Extract(Type t)
        {
            foreach (var attr in t.GetCustomAttributes())
            {
                if (attr is MoveName)
                {
                    return (attr as MoveName).Value;
                }
            }
            return null;
        }
    }

    public abstract class Move : BaseTransaction, IAction, ISerializable
    {
        private static string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss.ffffff";

        public new byte[] Creator => UserAddress;
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
                return MoveName.Extract(GetType());
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
                    { "created_at", Timestamp.ToString(TIMESTAMP_FORMAT) },
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var kv in PlainValue)
            {
                info.AddValue(kv.Key, kv.Value);
            }
            info.AddValue("signature", Signature.Hex());
        }

        public static Move FromPlainValue(IDictionary<string, dynamic> plainValue, Type type)
        {
            var move = Activator.CreateInstance(type) as Move;
            move.PublicKey = PublicKey.FromBytes((plainValue["user_public_key"] as string).ParseHex());
            move.Signature = (plainValue["signature"] as string).ParseHex();
            move.Tax = (int)plainValue["tax"];
            move.Details = plainValue["details"].ToObject<Dictionary<string, string>>();
            move.Timestamp = DateTime.ParseExact(
                plainValue["created_at"], TIMESTAMP_FORMAT, CultureInfo.InvariantCulture
            );
            return move;
        }
    }

    [Serializable]
    [MoveName("hack_and_slash")]
    public class HackAndSlash : Move
    {
        public HackAndSlash(Dictionary<string, string> details)
        {
            Details = details;
        }

        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            if (avatar.dead)
            {
                // TODO Implement InvalidMoveException
                throw new Exception();
            }
            throw new NotImplementedException();
        }
    }

    [Serializable]
    [MoveName("sleep")]
    public class Sleep : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    [MoveName("create_novice")]
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
                    class_ = CharacterClass.Novice.ToString(),
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

    [Serializable]
    [MoveName("first_class")]
    public class FirstClass : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            Dictionary<string, string> result = new Dictionary<string, string>
            {
                {"type", "first_class"},
                {"result", "success"}
            };
            if (avatar.class_ != CharacterClass.Novice.ToString())
            {
                result["result"] = "failed";
                result["message"] = "Already change class.";
                return new Tuple<Avatar, Dictionary<string, string>>(
                    avatar, result
                );
            }
            avatar.class_ = Details["class"];
            return new Tuple<Avatar, Dictionary<string, string>>(
                avatar, result
            );
        }
    }

    [Serializable]
    [MoveName("move_zone")]
    public class MoveZone : Move
    {
        public override Tuple<Avatar, Dictionary<string, string>> Execute(Avatar avatar)
        {
            throw new NotImplementedException();
        }
    }
}
