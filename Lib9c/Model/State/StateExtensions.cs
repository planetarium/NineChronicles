using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;

namespace Nekoyume.Model.State
{
    public static class StateExtensions
    {
        public static IValue Serialize<T>(Func<T, IValue> serializer, T? value)
            where T : struct
        {
            return value is T v ? serializer(v) : Null.Value;
        }

        public static T? Deserialize<T>(Func<IValue, T> deserializer, IValue serialized)
            where T : struct
        {
            return serialized is Null ? (T?)null : deserializer(serialized);
        }

        public static IValue Serialize<T>(this IEnumerable<T> values)
            where T : IValue
        {
            return new List(values.Cast<IValue>());
        }

        public static IEnumerable<T> ToEnumerable<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return ((List)serialized).Select(deserializer);
        }

        public static T[] ToArray<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return serialized.ToEnumerable(deserializer).ToArray();
        }

        public static List<T> ToList<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return serialized.ToEnumerable(deserializer).ToList();
        }

        public static HashSet<T> ToHashSet<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return new HashSet<T>(serialized.ToEnumerable(deserializer));
        }

        public static ImmutableHashSet<T> ToImmutableHashSet<T>(
            this IValue serialized,
            Func<IValue, T> deserializer
        ) => serialized.ToEnumerable(deserializer).ToImmutableHashSet();

        #region Address

        public static IValue Serialize(this Address address) =>
            address.Bencoded;

        public static IValue Serialize(this Address? address) =>
            Serialize(Serialize, address);

        public static Address ToAddress(this IValue serialized) =>
            new Address(serialized);

        public static Address? ToNullableAddress(this IValue serialized) =>
            Deserialize(ToAddress, serialized);

        #endregion

        #region Boolean

        public static IValue Serialize(this bool boolean) =>
            new Bencodex.Types.Boolean(boolean);

        public static IValue Serialize(this bool? boolean) =>
            Serialize(Serialize, boolean);

        public static bool ToBoolean(this IValue serialized) =>
            ((Bencodex.Types.Boolean)serialized).Value;

        public static bool? ToNullableBoolean(this IValue serialized) =>
            Deserialize(ToBoolean, serialized);

        #endregion

        //FIXME (Text) 대신 (Integer) 로 직렬화해야함
        #region Integer

        public static IValue Serialize(this int number) =>
            (Text)number.ToString(CultureInfo.InvariantCulture);

        public static IValue Serialize(this int? number) =>
            Serialize(Serialize, number);

        public static int ToInteger(this IValue serialized) =>
            int.Parse(((Text)serialized).Value, CultureInfo.InvariantCulture);

        public static int? ToNullableInteger(this IValue serialized) =>
            Deserialize(ToInteger, serialized);

        #endregion

        #region Long

        public static IValue Serialize(this long number) =>
            (Text)number.ToString(CultureInfo.InvariantCulture);

        public static IValue Serialize(this long? number) =>
            Serialize(Serialize, number);

        public static long ToLong(this IValue serialized) =>
            long.Parse(((Text)serialized).Value, CultureInfo.InvariantCulture);

        public static long? ToNullableLong(this IValue serialized) =>
            Deserialize(ToLong, serialized);

        #endregion

        #region BigInteger

        public static IValue Serialize(this BigInteger number) =>
            (Bencodex.Types.Integer)number;

        public static IValue Serialize(this BigInteger? number) =>
            Serialize(Serialize, number);

        public static BigInteger ToBigInteger(this IValue serialized) =>
            ((Bencodex.Types.Integer)serialized).Value;

        public static BigInteger? ToNullableBigInteger(this IValue serialized) =>
            Deserialize(ToBigInteger, serialized);

        #endregion

        #region Decimal

        public static IValue Serialize(this decimal number) =>
            (Text)number.ToString(CultureInfo.InvariantCulture);

        public static IValue Serialize(this decimal? number) =>
            Serialize(Serialize, number);

        public static decimal ToDecimal(this IValue serialized) =>
            decimal.Parse(((Text)serialized).Value, CultureInfo.InvariantCulture);

        public static decimal? ToNullableDecimal(this IValue serialized) =>
            Deserialize(ToDecimal, serialized);

        #endregion

        #region Text

        public static IValue Serialize(this string text) =>
            (Text)text;

        public static string ToDotnetString(this IValue serialized) => ((Text) serialized).Value;

        #endregion

        #region DateTimeOffset

        public static IValue Serialize(this DateTimeOffset dateTime) =>
            new Binary(
                Encoding.ASCII.GetBytes(dateTime.ToString("O", CultureInfo.InvariantCulture)));

        public static IValue Serialize(this DateTimeOffset? dateTime) =>
            Serialize(Serialize, dateTime);

        public static DateTimeOffset ToDateTimeOffset(this IValue serialized) =>
            DateTimeOffset.Parse(
                Encoding.ASCII.GetString(((Binary)serialized).ToByteArray()),
                null,
                DateTimeStyles.RoundtripKind
            );

        public static DateTimeOffset? ToNullableDateTimeOffset(this IValue serialized) =>
            Deserialize(ToDateTimeOffset, serialized);

        #endregion

        #region Guid

        public static IValue Serialize(this Guid number) =>
            new Binary(number.ToByteArray());

        public static IValue Serialize(this Guid? number) =>
            Serialize(Serialize, number);

        public static Guid ToGuid(this IValue serialized) =>
            new Guid(((Binary)serialized).ToByteArray());

        public static Guid? ToNullableGuid(this IValue serialized) =>
            Deserialize(ToGuid, serialized);

        #endregion

        #region DecimalStat

        public static IValue Serialize(this DecimalStat decimalStat) =>
            Dictionary.Empty
                .Add("type", StatTypeExtension.Serialize(decimalStat.Type))
                .Add("value", decimalStat.Value.Serialize());

        public static DecimalStat ToDecimalStat(this IValue serialized) =>
            ((Dictionary)serialized).ToDecimalStat();

        public static DecimalStat ToDecimalStat(this Dictionary serialized) =>
            new DecimalStat(
                StatTypeExtension.Deserialize((Binary)serialized["type"]),
                serialized["value"].ToDecimal());

        #endregion

        #region Generic

        public static IValue Serialize(this Dictionary<Material, int> value)
        {
            return
                new List(
                    value
                        .OrderBy(kv => kv.Key.Id)
                        .Select(
                            pair =>
                                (IValue)Dictionary.Empty
                                    .Add("material", pair.Key.Serialize())
                                    .Add("count", pair.Value.Serialize())
                        )
                );
        }

        public static Dictionary<Material, int> ToDictionary_Material_int(this IValue serialized)
        {
            return ((List)serialized)
                .Cast<Dictionary>()
                .ToDictionary(
                    value => (Material)ItemFactory.Deserialize((Dictionary)value["material"]),
                    value => value["count"].ToInteger()
                );
        }

        #endregion

        #region Bencodex.Types.Dictionary Getter

        public delegate bool IValueTryParseDelegate<T>(IValue input, out T output);

        public static T GetValue<T>(this Dictionary serialized, string key, T defaultValue, IValueTryParseDelegate<T> tryParser)
        {
            if (serialized.ContainsKey((IKey)(Text) key) &&
                tryParser(serialized[key], out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static Address GetAddress(this Dictionary serialized, string key, Address defaultValue = default)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToAddress()
                : defaultValue;
        }

        public static bool GetBoolean(this Dictionary serialized, string key, bool defaultValue = false)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToBoolean()
                : defaultValue;
        }

        public static int GetInteger(this Dictionary serialized, string key, int defaultValue = 0)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToInteger()
                : defaultValue;
        }

        public static long GetLong(this Dictionary serialized, string key, long defaultValue = 0L)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToLong()
                : defaultValue;
        }

        public static decimal GetDecimal(this Dictionary serialized, string key, decimal defaultValue = 0M)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToDecimal()
                : defaultValue;
        }

        public static string GetString(this Dictionary serialized, string key, string defaultValue = "")
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? ToDotnetString(serialized[key])
                : defaultValue;
        }

        public static DateTimeOffset GetDateTimeOffset(this Dictionary serialized, string key, DateTimeOffset defaultValue = default)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToDateTimeOffset()
                : defaultValue;
        }

        public static Guid GetGuid(this Dictionary serialized, string key, Guid defaultValue = default)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToGuid()
                : defaultValue;
        }

        public static DecimalStat GetDecimalStat(this Dictionary serialized, string key, DecimalStat defaultValue = default)
        {
            return serialized.ContainsKey((IKey)(Text)key)
                ? serialized[key].ToDecimalStat()
                : defaultValue;
        }

        #endregion

        #region PublicKey

        public static IValue Serialize(this PublicKey key) => new Binary(key.Format(true));

        public static PublicKey ToPublicKey(this IValue serialized) =>
            new PublicKey(((Binary)serialized).ByteArray);

        #endregion

        #region Enum

        public static IValue Serialize(this Enum type) => (Text) type.ToString();

        public static T ToEnum<T>(this IValue serialized) where T : struct
        {
            return (T) Enum.Parse(typeof(T), (Text) serialized);
        }

        #endregion

        #region HashDigest<SHA256>

        public static IValue Serialize(this HashDigest<SHA256> hashDigest) =>
            new Binary(hashDigest.ByteArray);

        public static HashDigest<SHA256> ToItemId(this IValue serialized)
        {
            return new HashDigest<SHA256>(((Binary)serialized).ByteArray);
        }

        #endregion

        #region FungibleAssetValue

        public static IValue Serialize(this FungibleAssetValue value) =>
            new Bencodex.Types.List(new IValue[]
            {
                CurrencyExtensions.Serialize(value.Currency),
                value.RawValue.Serialize(),
            });

        public static IValue Serialize(this FungibleAssetValue? value) =>
            Serialize(Serialize, value);

        public static FungibleAssetValue ToFungibleAssetValue(this IValue serialized) =>
            serialized is Bencodex.Types.List serializedList
                ? FungibleAssetValue.FromRawValue(
                    CurrencyExtensions.Deserialize(
                        (Bencodex.Types.Dictionary) serializedList.ElementAt(0)),
                    serializedList.ElementAt(1).ToBigInteger())
                : throw new InvalidCastException();

        public static FungibleAssetValue? ToNullableFungibleAssetValue(this IValue serialized) =>
            Deserialize(ToFungibleAssetValue, serialized);

        #endregion

        #region Buy

        public static PurchaseInfo0 ToPurchaseInfo(this IValue serialized) =>
            new PurchaseInfo0((Dictionary) serialized);

        public static BuyMultiple.PurchaseInfo ToPurchaseInfoLegacy(this IValue serialized) =>
            new BuyMultiple.PurchaseInfo((Dictionary) serialized);

        public static Buy7.PurchaseResult ToPurchaseResult(this IValue serialized) =>
            new Buy7.PurchaseResult((Dictionary) serialized);

        public static BuyMultiple.PurchaseResult ToPurchaseResultLegacy(this IValue serialized) =>
            new BuyMultiple.PurchaseResult((Dictionary) serialized);

        public static Buy7.SellerResult ToSellerResult(this IValue serialized) =>
            new Buy7.SellerResult((Dictionary) serialized);

        #endregion

        public static ILock ToLock(this IValue serialized)
        {
            var type = ((List) serialized).First().ToEnum<LockType>();
            switch (type)
            {
                case LockType.Order:
                    return new OrderLock((List) serialized);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
