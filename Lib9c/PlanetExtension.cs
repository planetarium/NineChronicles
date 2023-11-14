using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;

namespace Nekoyume
{
    public static class PlanetExtension
    {
        private static readonly BlockHash OdinGenesisHash = BlockHash.FromString(
            "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce"
        );

        // FIXME should be changed after Heimdall/Idun mainnet launches
        private static readonly BlockHash HeimdallGenesisHash = BlockHash.FromString(
            "ade4c29773fe83c1a51da6a667a5a26f08848155674637d43fe636b94a320514"
        );

        private static readonly BlockHash IdunGenesisHash = BlockHash.FromString(
            "209b22087045ec834f01249c8661c2734cea41ccc5d8c9a273a4c8c0521d22ec"
        );

        public static Planet? DeterminePlanet(this ITransaction tx)
        {
            // TODO Replace planet detection to using transaction payload instead.
            if (tx.GenesisHash.Equals(OdinGenesisHash))
            {
                return Planet.Odin;
            }
            if (tx.GenesisHash.Equals(HeimdallGenesisHash))
            {
                return Planet.Heimdall;
            }
            if (tx.GenesisHash.Equals(IdunGenesisHash))
            {
                return Planet.Idun;
            }

            return null;
        }
    }
}
