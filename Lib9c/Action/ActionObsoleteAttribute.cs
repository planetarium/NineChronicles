using System;

namespace Nekoyume.Action
{
    public class ActionObsoleteAttribute : Attribute
    {
        public ActionObsoleteAttribute(long obsoleteIndex)
        {
            ObsoleteIndex = obsoleteIndex;
        }

        public readonly long ObsoleteIndex;
    }
}
