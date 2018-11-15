using Nekoyume.Model;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Action;
using Planetarium.SDK.Address;
using Planetarium.SDK.Bencode;
using Planetarium.SDK.Tx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Nekoyume.Move
{
    internal class MoveName : Attribute
    {
        private string Value { get; set; }

        public MoveName(string value)
        {
            Value = value;
        }

        public static string Extract(Type t)
        {
            return t.GetCustomAttributes().OfType<MoveName>().Select(attr => (attr as MoveName).Value).FirstOrDefault();
        }
    }

    public abstract class Move : BaseTransaction, IAction
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        public new byte[] Creator => UserAddress;
        public new DateTime Timestamp { private get; set; }
        public byte[] UserAddress => PublicKey.ToAddress();
        public Dictionary<string, string> Details { protected get; set; }

        private string Name => MoveName.Extract(GetType());
        public int Tax { private get; set; }

        public long? BlockId { get; private set; }

        public bool Confirmed => BlockId.HasValue;

        public override IDictionary<string, dynamic> PlainValue => new Dictionary<string, dynamic>
        {
            { "user_address", "0x" + UserAddress.Hex() },
            { "name", Name },
            { "details", Details },
            { "created_at", Timestamp.ToString(TimestampFormat) },
            { "tax", Tax }
        };

        public bool Valid => Signature != null && PublicKey.Verify(Serialize(false), Signature);

        public new void Sign(PrivateKey privateKey)
        {
            PublicKey = privateKey.PublicKey;
            base.Sign(privateKey);
        }

        public abstract Context Execute(Context ctx);

        public static Move FromPlainValue(IDictionary<string, dynamic> plainValue, Type type)
        {
            var move = Activator.CreateInstance(type) as Move;
            Debug.Assert(move != null, nameof(move) + " != null");

            move.PublicKey = PublicKey.FromBytes((plainValue["user_public_key"] as string).ParseHex());
            move.Signature = (plainValue["signature"] as string).ParseHex();
            move.Tax = (int)plainValue["tax"];
            move.Details = plainValue["details"].ToObject<Dictionary<string, string>>();
            move.Timestamp = DateTime.ParseExact(
                plainValue["created_at"], TimestampFormat, CultureInfo.InvariantCulture
            );
            move.BlockId = plainValue["block"].ToObject<Dictionary<string, dynamic>>()["id"];
            return move;
        }

        public override byte[] Serialize(bool sign)
        {
            var values = PlainValue;

            if (sign)
            {
                values["signature"] = Signature;
                values["user_public_key"] = PublicKey.Format(true);
            }

            return values.ToBencoded();
        }
    }

    [MoveName("hack_and_slash")]
    public class HackAndSlash : Move
    {
        public override Context Execute(Context ctx)
        {
            if (ctx.avatar.dead)
            {
                throw new InvalidMoveException();
            }
            // TODO client battle result
            var result = new Dictionary<string, string>
            {
                {"type", "hack_and_slash"},
                {"result", "result"}
            };
            ctx.result = result;
            return ctx;
        }
    }

    [MoveName("sleep")]
    public class Sleep : Move
    {
        public override Context Execute(Context ctx)
        {
            ctx.avatar.hp = ctx.avatar.hp_max;
            ctx.result = new Dictionary<string, string>
            {
                    {"type", "sleep"},
                    {"result", "success"}
            };
            return ctx;
        }
    }

    [MoveName("create_novice")]
    public class CreateNovice : Move
    {
        public override Context Execute(Context ctx)
        {
            var result = new Dictionary<string, string>
            {
                {"type", "create_novice"},
                {"result", "success"}
            };
            ctx.avatar = new Avatar
            {
                name = Details["name"],
                user = UserAddress,
                gold = 0,
                class_ = CharacterClass.Novice.ToString(),
                level = 1,
                world_stage = 1,
                strength = 10,
                dexterity = 8,
                intelligence = 9,
                constitution = 9,
                luck = 9,
                hp_max = 10
            };
            return ctx;
        }
    }

    [MoveName("first_class")]
    public class FirstClass : Move
    {
        public override Context Execute(Context ctx)
        {
            var result = new Dictionary<string, string>
            {
                {"type", "first_class"},
                {"result", "success"}
            };
            ctx.result = result;
            if (ctx.avatar.class_ != CharacterClass.Novice.ToString())
            {
                ctx.result["result"] = "failed";
                ctx.result["message"] = "Already change class.";
                return ctx;
            }
            ctx.avatar.class_ = Details["class"];
            return ctx;
        }
    }

    [MoveName("move_zone")]
    public class MoveZone : Move
    {
        public override Context Execute(Context ctx)
        {
            throw new NotImplementedException();
        }
    }
}
