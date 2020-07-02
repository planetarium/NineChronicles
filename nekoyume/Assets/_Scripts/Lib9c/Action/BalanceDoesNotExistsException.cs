using Libplanet;
using System;

namespace Nekoyume.Action
{
    // FIXME Currency가 .NET Serializable 이 아니기 때문에 바로 직렬화 할 수 없습니다.
    // 직렬화 가능하게 고쳐둬야 합니다.
    [Serializable]
    public class BalanceDoesNotExistsException : Exception
    {
        public BalanceDoesNotExistsException(Address address, Currency currency)
        {
            Address = address;
            Currency = currency;
        }

        public Address Address { get; }
        public Currency Currency { get; }
    }
}
