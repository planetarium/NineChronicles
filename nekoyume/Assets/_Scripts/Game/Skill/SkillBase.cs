using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public enum SkillType
    {
        Attack,
        Buff,
        Debuff,
    }

    public interface ISkill
    {
        SkillType GetSkillType();
        Model.Attack Use();
    }

    public interface ISingleTargetSkill: ISkill
    {
        CharacterBase GetTarget();
    }

    [Serializable]
    public abstract class SkillBase: ISingleTargetSkill
    {
        protected readonly CharacterBase Caster;
        private readonly CharacterBase _target;
        protected readonly int Effect;

        public CharacterBase GetTarget() => _target;

        public abstract SkillType GetSkillType();

        public abstract Model.Attack Use();
        protected SkillBase(CharacterBase caster, CharacterBase target, int effect)
        {
            Caster = caster;
            _target = target;
            Effect = effect;
        }
    }
}
