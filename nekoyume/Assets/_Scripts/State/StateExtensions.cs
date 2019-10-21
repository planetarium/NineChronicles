using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.State
{
    public static class StateExtensions
    {
        public static IValue Serialize<T>(Func<T, IValue> serializer, T? value)
            where T : struct
        {
            return value is T v ? serializer(v) : default(Bencodex.Types.Null);
        }

        public static T? Deserialize<T>(Func<IValue, T> deserializer, IValue serialized)
            where T : struct
        {
            return serialized is Null ? (T?) null : deserializer(serialized);
        }

        public static IValue Serialize<T>(this IEnumerable<T> values)
            where T : IValue
        {
            return new Bencodex.Types.List(values.Cast<IValue>());
        }

        public static IEnumerable<T> ToEnumerable<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return ((Bencodex.Types.List) serialized).Select(deserializer);
        }

        public static T[] ToArray<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return serialized.ToEnumerable(deserializer).ToArray();
        }

        public static List<T> ToList<T>(this IValue serialized, Func<IValue, T> deserializer)
        {
            return serialized.ToEnumerable(deserializer).ToList();
        }

        public static IValue Serialize(this Address address) =>
            new Binary(address.ToByteArray());

        public static IValue Serialize(this Address? address) =>
            Serialize(Serialize, address);

        public static Address ToAddress(this IValue serialized) =>
            new Address(((Binary) serialized).Value);

        public static Address? ToNullableAddress(this IValue serialized) =>
            Deserialize(ToAddress, serialized);

        public static IValue Serialize(this decimal number) =>
            (Text) number.ToString(CultureInfo.InvariantCulture);

        public static IValue Serialize(this decimal? number) =>
            Serialize(Serialize, number);

        public static decimal ToDecimal(this IValue serialized) =>
            decimal.Parse(((Text) serialized).Value, CultureInfo.InvariantCulture);

        public static decimal? ToNullableDecimal(this IValue serialized) =>
            Deserialize(ToDecimal, serialized);

        public static IValue Serialize(this DateTimeOffset dateTime) =>
            new Binary(Encoding.ASCII.GetBytes(dateTime.ToString("O")));

        public static IValue Serialize(this DateTimeOffset? dateTime) =>
            Serialize(Serialize, dateTime);

        public static DateTimeOffset ToDateTimeOffset(this IValue serialized) =>
            DateTimeOffset.Parse(
                Encoding.ASCII.GetString(((Binary) serialized).Value),
                null,
                DateTimeStyles.RoundtripKind
            );

        public static DateTimeOffset? ToNullableDateTimeOffset(this IValue serialized) =>
            Deserialize(ToDateTimeOffset, serialized);

        public static IValue Serialize(this Guid number) =>
            new Binary(number.ToByteArray());

        public static IValue Serialize(this Guid? number) =>
            Serialize(Serialize, number);

        public static Guid ToGuid(this IValue serialized) =>
            new Guid(((Binary) serialized).Value);

        public static Guid? ToNullableGuid(this IValue serialized) =>
            Deserialize(ToGuid, serialized);
    }
}
