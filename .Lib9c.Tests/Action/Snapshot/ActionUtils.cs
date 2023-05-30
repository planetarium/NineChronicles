namespace Lib9c.Tests.Action.Snapshot
{
    using System;
    using System.Reflection;
    using Bencodex.Types;
    using Libplanet.Action;

    public static class ActionUtils
    {
        public static IValue GetActionTypeId<T>()
           where T : IAction
        {
            Type attrType = typeof(ActionTypeAttribute);
            Type actionType = typeof(T);
            return actionType.IsDefined(attrType) &&
                ActionTypeAttribute.ValueOf(actionType) is { } tid
                ? tid
                : throw new MissingActionTypeException("The action type", actionType);
        }
    }
}
