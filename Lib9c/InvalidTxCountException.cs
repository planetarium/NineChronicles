using Libplanet;
using Libplanet.Blocks;
using System;
using System.Runtime.Serialization;

namespace Lib9c
{
    [Serializable]
    public class InvalidTxCountException : InvalidBlockException
    {
        public int Maximum { get; }
        public int Actual { get; }
        public InvalidTxCountException(string message, int maximum, int actual)
            : base(message)
        {
            Maximum = maximum;
            Actual = actual;
        }

        public InvalidTxCountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Maximum = (int) info.GetValue(nameof(Maximum), typeof(int));
            Actual = (int) info.GetValue(nameof(Actual), typeof(int));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Maximum), Maximum);
            info.AddValue(nameof(Actual), Actual);
            
            base.GetObjectData(info, context);
        }
    }
}
