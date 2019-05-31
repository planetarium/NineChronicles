using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static SkillBase Get(CharacterBase caster, float chance, SkillEffect effect)
        {
            switch (effect.type)
            {
                case SkillEffect.SkillType.Attack:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Enemy:
                            switch (effect.category)
                            {
                                case SkillEffect.Category.Normal:
                                    return new Attack(caster, chance, effect, Data.Table.Elemental.ElementalType.Normal);
                                case SkillEffect.Category.Double:
                                    var values = Enum.GetValues(typeof(Data.Table.Elemental.ElementalType));
                                    var random = new Random();
                                    var elemental =
                                        (Data.Table.Elemental.ElementalType) values.GetValue(
                                            random.Next(values.Length));
                                    return new DoubleAttack(caster, chance, effect, elemental);
                                default:
                                    return new Attack(caster, chance, effect, Data.Table.Elemental.ElementalType.Normal);
                            }
                        case SkillEffect.Target.Enemies:
                            return new AreaAttack(caster, chance, effect, Data.Table.Elemental.ElementalType.Fire);
                    }
                    break;
                case SkillEffect.SkillType.Buff:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Self:
                            return new Heal(caster, chance, effect);
                    }
                    break;
                case SkillEffect.SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
