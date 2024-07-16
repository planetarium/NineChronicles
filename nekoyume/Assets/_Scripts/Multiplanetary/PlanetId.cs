#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nekoyume.Multiplanetary
{
    public readonly partial struct PlanetId : IEnumerable<byte>, IEquatable<PlanetId>
    {
        private const int Length = 12;

        private readonly byte[] _value;

        public PlanetId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.StartsWith("0x"))
            {
                value = value[2..];
            }

            if (value.Length != Length)
            {
                throw new ArgumentException(
                    $"Invalid length of {nameof(value)}: {value.Length} != {Length}");
            }

            _value = Encoding.Default.GetBytes(value);
        }

        public override string ToString()
        {
            return $"0x{ToHexString()}";
        }

        public string ToHexString()
        {
            return Encoding.Default.GetString(_value);
        }

        public IEnumerable<byte> ToEnumerable()
        {
            return _value;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(PlanetId other)
        {
            return _value.SequenceEqual(other);
        }

        public override bool Equals(object? obj)
        {
            return obj is PlanetId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}
