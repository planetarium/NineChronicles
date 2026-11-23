#nullable enable

using System.Globalization;

namespace Nekoyume
{
    /// <summary>
    /// Property value for <see cref="Analyzer.Track(string, System.Collections.Generic.Dictionary{string, AnalyticsValue}, bool)"/>.
    /// Holds the invariant-culture string form of the value that was passed in.
    /// </summary>
    public readonly struct AnalyticsValue
    {
        private readonly string? _text;

        private AnalyticsValue(string? text) => _text = text;

        public override string ToString() => _text ?? string.Empty;

        public static implicit operator AnalyticsValue(string? value) => new(value);
        public static implicit operator AnalyticsValue(bool value) => new(value.ToString());
        public static implicit operator AnalyticsValue(int value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(uint value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(long value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(ulong value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(short value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(ushort value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(byte value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(float value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(double value) => new(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator AnalyticsValue(decimal value) => new(value.ToString(CultureInfo.InvariantCulture));

        public static implicit operator AnalyticsValue(bool? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(int? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(uint? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(long? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(ulong? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(float? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(double? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
        public static implicit operator AnalyticsValue(decimal? value) => value.HasValue ? (AnalyticsValue)value.Value : default;
    }
}
