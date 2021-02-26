using Bencodex.Types;
using Libplanet;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class CreditsState : State
    {
        public static readonly Address Address = Addresses.Credits;
        
        public CreditsState(IEnumerable<string> names)
            : base(Addresses.Credits)
        {
            Names = names.ToImmutableList();
        }

        public CreditsState(Dictionary serialized)
            : base(serialized)
        {
            Names = ((List)serialized[nameof(Names)]).Select(s => s.ToDotnetString()).ToImmutableList();
        }

        public IImmutableList<string> Names { get; }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text)nameof(Names)] = new List(Names.Select(n => (Text) n).Cast<IValue>()),
            };
#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }
    }
}
