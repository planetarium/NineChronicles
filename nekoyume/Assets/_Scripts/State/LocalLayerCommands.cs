using System.Collections.Generic;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.Game;

namespace Nekoyume.State
{
    public class LocalLayerCommands
    {
        #region Singleton

        private static class Singleton
        {
            internal static readonly LocalLayerCommands Value = new LocalLayerCommands();
        }

        public static LocalLayerCommands Instance => Singleton.Value;

        private LocalLayerCommands()
        {
        }

        #endregion

        public void Apply(
            IAgent agent,
            States states,
            TableSheets tableSheets,
            IReadOnlyList<Address> updatedAddresses,
            bool ignoreNotify = false)
        {
            // TODO: implement.
        }
    }
}
