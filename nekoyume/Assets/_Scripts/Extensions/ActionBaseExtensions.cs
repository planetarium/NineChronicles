using System;
using Libplanet.Action;
using Nekoyume.Action;

namespace Nekoyume
{
    public static class ActionBaseExtensions
    {
        public static ActionTypeAttribute GetActionTypeAttribute(this ActionBase actionBase)
        {
            var gameActionType = actionBase.GetType();
            return (ActionTypeAttribute)Attribute.GetCustomAttribute(
                gameActionType,
                typeof(ActionTypeAttribute));
        }
    }
}
