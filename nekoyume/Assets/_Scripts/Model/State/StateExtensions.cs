using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;

namespace Nekoyume.Model.State
{
    public static class StateExtensions
    {
        public static IValue Serialize<T>(Func<T, IValue> serializer, T? value)
            where T : struct
        {
            return value is T v ? serializer(v) : default(Null);
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

        #region Address

        public static IValue Serialize(this Address address) =>
            new Binary(address.ToByteArray());

        public static IValue Serialize(this Address? address) =>
            Serialize(Serialize, address);

        public static Address ToAddress(this IValue serialized) =>
            new Address(((Binary)serialized).Value);

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
            (Text)number.ToString();

        public static IValue Serialize(this int? number) =>
            Serialize(Serialize, number);

        public static int ToInteger(this IValue serialized) =>
            int.Parse(((Text)serialized).Value, CultureInfo.InvariantCulture);

        public static int? ToNullableInteger(this IValue serialized) =>
            Deserialize(ToInteger, serialized);

        #endregion

        #region Long

        public static IValue Serialize(this long number) =>
            (Text)number.ToString();

        public static IValue Serialize(this long? number) =>
            Serialize(Serialize, number);

        public static long ToLong(this IValue serialized) =>
            int.Parse(((Text)serialized).Value, CultureInfo.InvariantCulture);

        public static long? ToNullableLong(this IValue serialized) =>
            Deserialize(ToInteger, serialized);

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

        public static string ToString(this IValue serialized) =>
            ((Text)serialized).Value;

        #endregion

        #region DateTimeOffset

        public static IValue Serialize(this DateTimeOffset dateTime) =>
            new Binary(Encoding.ASCII.GetBytes(dateTime.ToString("O")));

        public static IValue Serialize(this DateTimeOffset? dateTime) =>
            Serialize(Serialize, dateTime);

        public static DateTimeOffset ToDateTimeOffset(this IValue serialized) =>
            DateTimeOffset.Parse(
                Encoding.ASCII.GetString(((Binary)serialized).Value),
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
            new Guid(((Binary)serialized).Value);

        public static Guid? ToNullableGuid(this IValue serialized) =>
            Deserialize(ToGuid, serialized);

        #endregion

        #region DecimalStat

        public static IValue Serialize(this DecimalStat decimalStat) =>
            Dictionary.Empty
                .Add("type", decimalStat.Type.Serialize())
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
                    value.Select(
                        pair =>
                        (IValue)Dictionary.Empty
                            .Add("material", pair.Key.Serialize())
                            .Add("count", pair.Value.Serialize())));
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

        public static Address GetAddress(this Dictionary serialized, string key, Address defaultValue = default)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToAddress()
                : defaultValue;
        }

        public static bool GetBoolean(this Dictionary serialized, string key, bool defaultValue = false)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToBoolean()
                : defaultValue;
        }

        public static int GetInteger(this Dictionary serialized, string key, int defaultValue = 0)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToInteger()
                : defaultValue;
        }

        public static long GetLong(this Dictionary serialized, string key, long defaultValue = 0L)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToLong()
                : defaultValue;
        }

        public static decimal GetDecimal(this Dictionary serialized, string key, decimal defaultValue = 0M)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToDecimal()
                : defaultValue;
        }

        public static string GetString(this Dictionary serialized, string key, string defaultValue = "")
        {
            return serialized.ContainsKey((Text)key)
                ? ToString(serialized[key])
                : defaultValue;
        }

        public static DateTimeOffset GetDateTimeOffset(this Dictionary serialized, string key, DateTimeOffset defaultValue = default)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToDateTimeOffset()
                : defaultValue;
        }

        public static Guid GetGuid(this Dictionary serialized, string key, Guid defaultValue = default)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToGuid()
                : defaultValue;
        }

        public static DecimalStat GetDecimalStat(this Dictionary serialized, string key, DecimalStat defaultValue = default)
        {
            return serialized.ContainsKey((Text)key)
                ? serialized[key].ToDecimalStat()
                : defaultValue;
        }

        #endregion
    }
}
