using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Buff
{
    [Serializable]
    public abstract class Buff
    {
        public BuffSheet.Row data;
        public int effect;
        public int time;
        public int chance;
        private readonly SkillTargetType _targetType;
        public abstract BuffCategory Category { get; }
        public abstract int Use(CharacterBase characterBase);

        protected Buff(BuffSheet.Row row)
        {
            data = row;
            effect = row.effect;
            time = row.time;
            chance = row.chance;
            _targetType = row.targetType;
        }

        public IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            var targets = caster.targets;
            IEnumerable<CharacterBase> target;
            switch (_targetType)
            {
                case SkillTargetType.Enemy:
                    target = new[] {targets.First()};
                    break;
                case SkillTargetType.Enemies:
                    target = caster.targets;
                    break;
                case SkillTargetType.Self:
                    target = new[] {caster};
                    break;
                case SkillTargetType.Ally:
                    target = new[] {caster};
                    break;
                default:
                    target = new[] {targets.First()};
                    break;
            }

            return target;

        }
    }
}
