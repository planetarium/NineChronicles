using Nekoyume.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Nekoyume.Battle
{
    public static class CriticalHelper
    {
        private const decimal MinimumDamageMultiplier = 1m;

        public static int GetCriticalDamage(CharacterBase caster, int originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    CharacterBase.CriticalMultiplier + (caster.CDMG / 10000m));
            return (int)(originalDamage * critMultiplier);
        }

        public static int GetCriticalDamageForArena(ArenaCharacter caster, int originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    ArenaCharacter.CriticalMultiplier + (caster.CDMG / 10000m));
            return (int)(originalDamage * critMultiplier);
        }
    }
}
