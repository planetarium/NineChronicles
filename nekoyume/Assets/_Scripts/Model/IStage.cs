using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);

        #region Skill

        IEnumerator CoNormalAttack(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(CharacterBase caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        
        #endregion
        
        IEnumerator CoRemoveBuffs(CharacterBase caster);
        
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Enemy> enemies, bool hasBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoWaveTurnEnd(int waveTurn, int turn);
    }
}
