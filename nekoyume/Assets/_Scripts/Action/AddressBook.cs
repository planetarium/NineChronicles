using Libplanet;
using UniRx;
using UnityEngine;

namespace Nekoyume.Action
{
    public static class AddressBook
    {
        public static readonly Address Shop = default(Address);
        public static readonly Address Ranking = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1
            }
        );
        public static readonly ReactiveProperty<Address> Agent = new ReactiveProperty<Address>();
        public static readonly ReactiveProperty<Address> Avatar = new ReactiveProperty<Address>();

        static AddressBook()
        {
            Agent.Subscribe(address => Debug.Log($"Agent Address: 0x{address.ToHex()}"));
            Avatar.Subscribe(address => Debug.Log($"Avatar Address: 0x{address.ToHex()}"));
        }
    }
}
