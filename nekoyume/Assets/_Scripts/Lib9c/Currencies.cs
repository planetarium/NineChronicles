using Libplanet;

namespace Nekoyume
{
    public static class Currencies
    {
        /// <summary>
        /// NCG (Nine Chronicles Gold).
        /// </summary>
        // FIXME: minter 지정해야 함.
        public static readonly Currency Gold = new Currency("NCG", minter: null);
    }
}
