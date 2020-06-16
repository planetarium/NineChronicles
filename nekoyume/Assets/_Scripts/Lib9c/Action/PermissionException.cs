using Nekoyume.Model.State;
using System;

namespace Nekoyume.Action
{
    [Serializable]
    abstract public class AdminPermissionException : Exception
    {
        public AdminState Policy { get; private set; }

        public AdminPermissionException(AdminState policy)
        {
            Policy = policy;
        }
    }
}
