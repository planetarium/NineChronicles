using System;
using Libplanet.Action;
using Nekoyume.Action;

namespace Nekoyume
{
    public static class GameActionExtensions
    {
        public static ActionTypeAttribute GetActionTypeAttribute(this GameAction gameAction)
        {
            var gameActionType = gameAction.GetType();
            return (ActionTypeAttribute)Attribute.GetCustomAttribute(gameActionType, typeof(ActionTypeAttribute));
        }
    }
}
