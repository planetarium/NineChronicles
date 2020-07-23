using System;

namespace Nekoyume.Action
{
    public class AlreadyReceivedException : InvalidOperationException
    {
        public AlreadyReceivedException(string s) : base(s)
        {
        }
    }
}
