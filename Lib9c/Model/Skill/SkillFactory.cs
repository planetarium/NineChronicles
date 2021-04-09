using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(SkillSheet.Row skillRow, int power, int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance);
                        default:
                            return new NormalAttack(skillRow, power, chance);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance);
                // todo: 코드상에서 버프와 디버프를 버프로 함께 구분하고 있는데, 고도화 될 수록 디버프를 구분해주게 될 것으로 보임.
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        public static Skill Deserialize(Dictionary serialized)
        {
            bool useLegacy = serialized.ContainsKey(LegacySkillRowKey);
            string skillRowKey = useLegacy ? LegacySkillRowKey : SkillRowKey;
            string powerKey = useLegacy ? LegacyPowerKey : PowerKey;
            string chanceKey = useLegacy ? LegacyChanceKey : ChanceKey;
            return Get(
                SkillSheet.Row.Deserialize((Dictionary) serialized[skillRowKey]),
                serialized[powerKey].ToInteger(),
                serialized[chanceKey].ToInteger()
            );
        }
    }
}
