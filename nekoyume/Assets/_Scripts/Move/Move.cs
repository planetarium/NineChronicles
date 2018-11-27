using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Nekoyume.Model;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Address;
using Planetarium.SDK.Bencode;
using Planetarium.SDK.Tx;
using Avatar = Nekoyume.Model.Avatar;
using Debug = System.Diagnostics.Debug;

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

    internal class Preprocess : Attribute
    {
    }

    public abstract class Move : BaseTransaction
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
            {"user_address", "0x" + UserAddress.Hex()},
            {"name", Name},
            {"details", Details},
            {"created_at", Timestamp.ToString(TimestampFormat)},
            {"tax", Tax}
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
            move.Tax = (int) plainValue["tax"];
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

        protected Context CreateContext(ContextStatus status = ContextStatus.Success, Avatar avatar = null)
        {
            return new Context
            {
                Status = status,
                Type = Name,
                Avatar = avatar
            };
        }
    }

    [MoveName("hack_and_slash")]
    [Preprocess]
    public class HackAndSlash : Move
    {
        public override Context Execute(Context ctx)
        {
            if (ctx.Avatar.dead)
            {
                throw new InvalidMoveException();
            }
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.hp = int.Parse(Details["hp"]);
            newCtx.Avatar.world_stage = int.Parse(Details["stage"]);
            newCtx.Avatar.dead = Details["dead"].ToLower() == "true";
            newCtx.Avatar.exp = int.Parse(Details["exp"]);
            newCtx.Avatar.level = int.Parse(Details["level"]);
            return newCtx;
        }
    }

    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : Move
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.dead = false;
            string data;
            int hp;
            Details.TryGetValue("hp", out data);
            int.TryParse(data, out hp);
            newCtx.Avatar.hp = hp;
            return newCtx;
        }
    }

    [MoveName("create_novice")]
    public class CreateNovice : Move
    {
        public override Context Execute(Context ctx)
        {
            return CreateContext(
                ContextStatus.Success,
                new Avatar
                {
                    name = Details["name"],
                    user = UserAddress,
                    gold = 0,
                    class_ = CharacterClass.Novice.ToString(),
                    level = 1,
                    world_stage = 1
                }
            );
        }
    }

    [MoveName("first_class")]
    public class FirstClass : Move
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = new Context
            {
                Type = "first_class"
            };

            if (ctx.Avatar.class_ != CharacterClass.Novice.ToString())
            {
                newCtx.Status = ContextStatus.Failed;
                newCtx.Message = "Already change class.";
                return newCtx;
            }

            newCtx.Status = ContextStatus.Success;
            newCtx.Avatar.class_ = Details["class_"];
            return newCtx;
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
