using System;
using Libplanet.Action;

namespace Nekoyume.Action
{
    /// <summary>
    /// <para>
    /// An attribute on an <see cref="IAction"/> to indicate that
    /// such <see cref="IAction"/> is obsoleted after a certain index.
    /// </para>
    /// <para>
    /// Due to a bug introduced earlier and to keep backward compatibility,
    /// an <see cref="IAction"/> with this attribute is obsoleted, i.e. cannot be included,
    /// starting from <see cref="ActionObsoleteAttribute.ObsoleteIndex"/> + 2.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ActionObsoleteAttribute : Attribute
    {
        public ActionObsoleteAttribute(long obsoleteIndex)
            : this(null, obsoleteIndex)
        {
        }

        public ActionObsoleteAttribute(Planet planet, long obsoleteIndex)
            : this((Planet?)planet, obsoleteIndex)
        {
        }

        private ActionObsoleteAttribute(Planet? planet, long obsoleteIndex)
        {
            AffectedPlanet = planet;
            ObsoleteIndex = obsoleteIndex;
        }

        public readonly long ObsoleteIndex;

        public readonly Planet? AffectedPlanet;
    }
}
