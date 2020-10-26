using Nekoyume.Model.State;
using System;
using System.Runtime.Serialization;
using Libplanet.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class AdminPermissionException : Exception
    {
        public AdminState Policy { get; private set; }

        public AdminPermissionException(AdminState policy)
        {
            Policy = policy;
        }

        protected AdminPermissionException(SerializationInfo info, StreamingContext context)
        {
            Policy = info.GetValue<AdminState>(nameof(Policy));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(Policy), Policy);
        }
    }
}
