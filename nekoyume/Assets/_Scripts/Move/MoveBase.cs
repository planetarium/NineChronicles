using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Nekoyume.Action;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Tx;
using Avatar = Nekoyume.Model.Avatar;
using Debug = System.Diagnostics.Debug;
using ShouldBeRemoved;

// TODO: It should be removed when alternative is implemented
public abstract class BaseTransaction
{
    public byte[] Id {
        get
        {
            var serialized = Serialize(true);
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(serialized);
            }
        }
    }

    public byte[] Creator { get; protected set; }

    public PublicKey PublicKey { get; protected set; }

    public DateTime Timestamp { get; protected set; }

    public byte[] Signature { get; protected set; }

    public abstract IDictionary<string, dynamic> PlainValue { get;  }

    public virtual byte[] Serialize(bool sign)
    {
        var values = PlainValue;
        if (sign)
        {
            values["signature"] = Signature;
        }
        return values.ToBencoded();
    }

    public void Sign(PrivateKey privateKey)
    {
        Signature = privateKey.Sign(Serialize(false));
    }
}

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

        public new Address Creator => UserAddress;
        public new DateTime Timestamp { private get; set; }
        public Address UserAddress => PublicKey.ToAddress();
        public Dictionary<string, string> Details { protected get; set; }

        private string Name => MoveName.Extract(GetType());
        public int Tax { private get; set; }

        public long? BlockId { get; private set; }

        public bool Confirmed => BlockId.HasValue;

        public IEnumerable<ActionBase> Actions;

        public override IDictionary<string, dynamic> PlainValue => new Dictionary<string, dynamic>
        {
            {"user_address", "0x" + UserAddress},
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

        public static MoveBase FromPlainValue(IDictionary<string, dynamic> plainValue, Type type)
        {
            var move = Activator.CreateInstance(type) as MoveBase;
            Debug.Assert(move != null, nameof(move) + " != null");

            move.PublicKey = new PublicKey(ByteUtil.ParseHex((plainValue["user_public_key"] as string)));
            move.Signature = ByteUtil.ParseHex((plainValue["signature"] as string));
            move.Tax = (int) plainValue["tax"];
            var details = plainValue["details"].ToObject<Dictionary<string, string>>();
            switch (move.Name)
            {
                case "create_novice":
                    move.Actions = new[] {new Action.CreateNovice(details["name"])};
                    break;
                case "sleep":
                    move.Actions = new[] {new Action.Sleep()};
                    break;
            }
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
}
