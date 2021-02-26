using System;
using System.Runtime.Serialization;

namespace NineChronicles.RPC.Shared.Exceptions
{
    public enum RPCException
    {
        NetworkException = 0x01,
        
        ChainTooLowException = 0x02,

        // Used by ValidatingActionRenderer<T> (i.e., --strict-rendering):
        InvalidRenderException = 0x03,
    }
}
