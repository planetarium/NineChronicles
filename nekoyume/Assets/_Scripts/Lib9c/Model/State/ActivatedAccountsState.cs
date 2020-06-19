using Bencodex;
using Bencodex.Types;
using Libplanet;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class ActivatedAccountsState : State, ISerializable
    {
        public static readonly Address Address = new Address(
            new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x8,
            }
        );

        public IImmutableSet<Address> Accounts { get; private set; }

        public ActivatedAccountsState()
            : this(ImmutableHashSet<Address>.Empty)
        {
        }

        public ActivatedAccountsState(IImmutableSet<Address> accounts)
            : base(Address)
        {
            Accounts = accounts;
        }

        public ActivatedAccountsState(Dictionary serialized)
            : base(serialized)
        {
            Accounts = serialized["accounts"]
                .ToList(a => a.ToAddress())
                .ToImmutableHashSet();
        }

        protected ActivatedAccountsState(SerializationInfo info, StreamingContext context)
            : this((Dictionary)new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public ActivatedAccountsState AddAccount(Address account)
        {
            return new ActivatedAccountsState(Accounts.Add(account));
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text)"accounts"] = Accounts.Select(a => a.Serialize()).Serialize()
            };
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }
    }
}
