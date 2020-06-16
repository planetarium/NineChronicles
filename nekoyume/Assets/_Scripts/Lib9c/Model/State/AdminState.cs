using Bencodex.Types;
using Libplanet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class AdminState : State
    {
        public static readonly Address Address = new Address(
            new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x6
            }
        );

        public Address AdminAddress { get; private set; }

        public long ValidUntil { get; private set; }

        public AdminState(Address adminAddress, long validUntil)
            : base(Address)
        {
            AdminAddress = adminAddress;
            ValidUntil = validUntil;
        }
        public AdminState(Dictionary serialized) : base(serialized)
        {
            AdminAddress = serialized["admin_address"].ToAddress();
            ValidUntil = serialized["valid_until"].ToLong();
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "admin_address"] = AdminAddress.Serialize(),
                [(Text) "valid_until"] = ValidUntil.Serialize(),
            };
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
        }

    }
}
