using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;

namespace Nekoyume.Model
{
    public interface IArena
    {
        IEnumerator CoSpawnCharacter(ArenaCharacter character);

        IEnumerator CoNormalAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBlowAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoAreaAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBuffRemovalAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoHeal(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBuff(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoTickDamage(ArenaCharacter affectedCharacter, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos);
        IEnumerator CoRemoveBuffs(ArenaCharacter caster);
        IEnumerator CoDead(ArenaCharacter caster);
        IEnumerator CoTurnEnd(int turnNumber);
    }
}
