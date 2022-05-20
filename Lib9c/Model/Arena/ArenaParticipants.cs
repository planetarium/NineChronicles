using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1027
    /// </summary>
    public class ArenaParticipants : IState
    {
        public static Address DeriveAddress(int championshipId, int round) =>
            Addresses.Arena.Derive($"arena_participants_{championshipId}_{round}");

        public Address Address;
        public readonly List<Address> AvatarAddresses;

        public ArenaParticipants(int championshipId, int round)
        {
            Address = DeriveAddress(championshipId, round);
            AvatarAddresses = new List<Address>();
        }

        public ArenaParticipants(List serialized)
        {
            Address = serialized[0].ToAddress();
            AvatarAddresses = ((List)serialized[1]).Select(c => c.ToAddress()).ToList();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(AvatarAddresses.Aggregate(List.Empty,
                    (list, address) => list.Add(address.Serialize())));
        }

        public void Add(Address address)
        {
            AvatarAddresses.Add(address);
        }
    }
}
