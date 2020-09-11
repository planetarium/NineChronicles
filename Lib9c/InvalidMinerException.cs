using Libplanet;
using Libplanet.Blocks;
using System;
using System.Runtime.Serialization;

namespace Lib9c
{
    [Serializable]
    public class InvalidMinerException : InvalidBlockException
    {
        public Address Miner { get; }
        public InvalidMinerException(string message, Address miner)
            : base(message)
        {
            Miner = miner;
        }

        // FIXME We should call base(info, context) here, 
        // but InvalidBlockException doesn't have a constructor for them yet.
        public InvalidMinerException(SerializationInfo info, StreamingContext context)
            : base(info.GetString(nameof(Message)))
        {
            Miner = (Address) info.GetValue(nameof(Miner), typeof(Address));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Message), Message);
            info.AddValue(nameof(Miner), Miner);
        }
    }
}
