namespace Nekoyume.Model.Skill
{
    public enum SkillCategory
    {
        NormalAttack,
        BlowAttack,
        DoubleAttack,
        AreaAttack,
        BuffRemovalAttack,

        Heal,

        // todo: 코드상에서 버프와 디버프를 버프로 함께 구분하고 있는데, 고도화 될 수록 디버프를 구분해주게 될 것으로 보임.
        HPBuff,
        AttackBuff,
        DefenseBuff,
        CriticalBuff,
        HitBuff,
        SpeedBuff,
        DamageReductionBuff,
        CriticalDamageBuff,
        Buff,
        Debuff,
    }
}
