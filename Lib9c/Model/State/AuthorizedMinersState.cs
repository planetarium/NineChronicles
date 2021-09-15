using Bencodex.Types;
using Libplanet;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.Model.State
{
    public class AuthorizedMinersState : State
    {
        public readonly static Address Address = Addresses.AuthorizedMiners;

        public long Interval { get; private set; }

        public ImmutableHashSet<Address> Miners { get; private set; }

        public long ValidUntil { get; private set; }

        public AuthorizedMinersState(
            IEnumerable<Address> miners,
            long interval,
            long validUntil
        ) : base(Address)
        {
            Miners = miners.ToImmutableHashSet();
            Interval = interval;
            ValidUntil = validUntil;
        }

        public AuthorizedMinersState(Dictionary serialized)
            : base(serialized)
        {
            Miners = ((List)serialized[nameof(Miners)]).Select(v => v.ToAddress())
                .ToImmutableHashSet();
            Interval = serialized[nameof(Interval)].ToLong();
            ValidUntil = serialized[nameof(ValidUntil)].ToLong();
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            var values = new Dictionary<IKey, IValue>
            {
                [(Text)nameof(Miners)] = new List(Miners.Select(m => m.Serialize())),
                [(Text)nameof(Interval)] = Interval.Serialize(),
                [(Text)nameof(ValidUntil)] = ValidUntil.Serialize(),
            };
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }
    }
}
