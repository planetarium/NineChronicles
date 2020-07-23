using System;

namespace Nekoyume.Action
{
    public class ArenaNotEndedException : InvalidOperationException
    {
        public ArenaNotEndedException(string s) : base(s)
        {
        }
    }
}
