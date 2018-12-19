using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Nekoyume.Action;
using Newtonsoft.Json.Linq;
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

    public abstract class MoveBase : BaseTransaction
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

        public IEnumerable<ActionBase> Actions;

        public override IDictionary<string, object> PlainValue => new Dictionary<string, object>
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

        public static MoveBase FromPlainValue(IDictionary<string, object> plainValue, Type type)
        {
            var move = Activator.CreateInstance(type) as MoveBase;
            Debug.Assert(move != null, nameof(move) + " != null");

            move.PublicKey = PublicKey.FromBytes((plainValue["user_public_key"] as string).ParseHex());
            move.Signature = (plainValue["signature"] as string).ParseHex();
            move.Tax = Convert.ToInt32(plainValue["tax"]);
            move.Details = ((JObject)plainValue["details"]).ToObject<Dictionary<string, string>>();
            switch (move.Name)
            {
                case "create_novice":
                    move.Actions = new[] { new Action.CreateNovice(move.Details["name"]) };
                    break;
                case "sleep":
                    move.Actions = new[] { new Action.Sleep() };
                    break;
            }
            move.Timestamp = DateTime.ParseExact(
                (string)plainValue["created_at"], TimestampFormat, CultureInfo.InvariantCulture
            );
            var block = (JObject)plainValue["block"];
            move.BlockId = (long)block.GetValue("id");
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
}
