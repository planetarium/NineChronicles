using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public abstract class Buff : ICloneable
    {
        public int remainedDuration;

        public BuffSheet.Row Data { get; }

        protected Buff(BuffSheet.Row row)
        {
            remainedDuration = row.Duration;
            Data = row;
        }

        public abstract int Use(CharacterBase characterBase);

        public IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            var targets = caster.targets;
            IEnumerable<CharacterBase> target;
            switch (Data.TargetType)
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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
