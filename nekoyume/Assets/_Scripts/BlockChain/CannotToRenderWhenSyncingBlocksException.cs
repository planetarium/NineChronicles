using System;
using System.Runtime.Serialization;

namespace Nekoyume.BlockChain
{
    [Serializable]
    public class CannotToRenderWhenSyncingBlocksException : Exception
    {
        public CannotToRenderWhenSyncingBlocksException() : base()
        {
        }

        public CannotToRenderWhenSyncingBlocksException(string message) : base(message)
        {
        }

        public CannotToRenderWhenSyncingBlocksException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
