using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;

namespace Nekoyume
{
    public static class ActionObsoleteAttributeExtension
    {
        public static bool IsObsolete(
            this IEnumerable<ActionObsoleteAttribute> attrs,
            Planet planet,
            long blockIndex
        )
        {
            // it means that action had been obsoleted in the single planet era.
            // in that case, check index in Odin only, rest should be obsoleted.
            if (attrs.Count() == 1)
            {
                if (planet != Planet.Odin)
                {
                    return true;
                }

                // Comparison with ObsoleteIndex + 2 is intended to have backward compatibility with
                // a bugged original implementation.
                return attrs.Single().ObsoleteIndex + 2 <= blockIndex;
            }

            return attrs.Any(attr => (attr.AffectedPlanet == Planet.Odin ? attr.ObsoleteIndex + 2 : attr.ObsoleteIndex) <= blockIndex && attr.AffectedPlanet == planet);
        }
    }
}
