using System;

namespace Nekoyume.Model.Skill
{
    public class UnexpectedOperationException : Exception
    {
        public UnexpectedOperationException(string message) : base(message)
        {
        }
    }
}
