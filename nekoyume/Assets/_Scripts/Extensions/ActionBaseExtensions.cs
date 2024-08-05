using System;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Buff;
using Buff = Nekoyume.Model.BattleStatus.Buff;

namespace Nekoyume
{
    public static class ActionBaseExtensions
    {
        public static ActionTypeAttribute GetActionTypeAttribute(this ActionBase actionBase)
        {
            var gameActionType = actionBase.GetType();
            return (ActionTypeAttribute)Attribute.GetCustomAttribute(
                gameActionType,
                typeof(ActionTypeAttribute));
        }

        public static void LogEvent(this EventBase e, int eventIndex, int eventCount)
        {
#if !DEBUG_USE
            return;
#endif

            var sb = new System.Text.StringBuilder();

            switch (e)
            {
                case Dead dead:
                    sb.AppendLine($"OnDead: {dead.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {dead.Character.RowData.Id}");
                    break;
                case BlowAttack blowAttack:
                    sb.AppendLine($"OnBlowAttack: {blowAttack.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {blowAttack.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {blowAttack.SkillId}");
                    break;
                case Buff buff:
                    sb.AppendLine($"OnBuff: {buff.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {buff.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {buff.SkillId}");
                    sb.AppendLine("Character Stat: ");
                    sb.AppendLine($"- ATK: {buff.Character.ATK}");
                    sb.AppendLine($"- DEF: {buff.Character.DEF}");
                    sb.AppendLine($"- HIT: {buff.Character.HIT}");
                    sb.AppendLine($"- SPD: {buff.Character.SPD}");
                    sb.AppendLine($"- DRV: {buff.Character.DRV}");
                    sb.AppendLine($"- DRR: {buff.Character.DRR}");
                    sb.AppendLine($"- CDMG: {buff.Character.CDMG}");
                    if (buff.BuffInfos != null)
                    {
                        foreach (var buffInfo in buff.BuffInfos)
                        {
                            if (buffInfo.Buff == null)
                            {
                                continue;
                            }

                            sb.AppendLine($"- has buff: {buffInfo.Buff.BuffInfo.Id}");
                            sb.AppendLine($"  - GroupId: {buffInfo.Buff.BuffInfo.GroupId}");
                            sb.AppendLine($"  - Chance: {buffInfo.Buff.BuffInfo.Chance}");
                            sb.AppendLine($"  - Duration: {buffInfo.Buff.BuffInfo.Duration}");
                        }
                    }

                    break;
                case AreaAttack areaAttack:
                    sb.AppendLine($"OnAreaAttack: {areaAttack.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {areaAttack.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {areaAttack.SkillId}");
                    foreach (var skill in areaAttack.SkillInfos)
                    {
                        sb.AppendLine($"- has skill: {skill}");
                        sb.AppendLine($"  - skillCategory: {skill.SkillCategory}");
                        sb.AppendLine($"  - id: {skill.Target?.Id}");
                    }

                    break;
                case DoubleAttack doubleAttack:
                    sb.AppendLine($"OnDoubleAttack: {doubleAttack.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {doubleAttack.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {doubleAttack.SkillId}");
                    foreach (var skill in doubleAttack.SkillInfos)
                    {
                        sb.AppendLine($"- has skill: {skill}");
                        sb.AppendLine($"  - skillCategory: {skill.SkillCategory}");
                        sb.AppendLine($"  - id: {skill.Target?.Id}");
                    }

                    break;
                case DoubleAttackWithCombo doubleAttackWithCombo:
                    sb.AppendLine($"OnDoubleAttackWithCombo: {doubleAttackWithCombo.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {doubleAttackWithCombo.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {doubleAttackWithCombo.SkillId}");
                    foreach (var skill in doubleAttackWithCombo.SkillInfos)
                    {
                        sb.AppendLine($"- has skill: {skill}");
                        sb.AppendLine($"  - skillCategory: {skill.SkillCategory}");
                        sb.AppendLine($"  - id: {skill.Target?.Id}");
                    }

                    break;
                case HealSkill healSkill:
                    sb.AppendLine($"OnHealSkill: {healSkill.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {healSkill.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {healSkill.SkillId}");
                    break;
                case NormalAttack normalAttack:
                    sb.AppendLine($"OnNormalAttack: {normalAttack.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {normalAttack.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {normalAttack.SkillId}");
                    foreach (var skill in normalAttack.SkillInfos)
                    {
                        sb.AppendLine($"- has skill: {skill}");
                        sb.AppendLine($"  - skillCategory: {skill.SkillCategory}");
                        sb.AppendLine($"  - id: {skill.Target?.Id}");
                    }

                    break;
                case RemoveBuffs removeBuffs:
                    sb.AppendLine($"OnRemoveBuffs: {removeBuffs.Character.Id} ({eventIndex}/{eventCount})");
                    break;
                case Tick tick:
                    sb.AppendLine($"OnTick: {tick.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {tick.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {tick.SkillId}");
                    if (AuraIceShield.IsFrostBiteBuff(tick.SkillId))
                    {
                        foreach (var kvp in tick.Character.Buffs)
                        {
                            if (!AuraIceShield.IsFrostBiteBuff(kvp.Key))
                            {
                                continue;
                            }

                            if (kvp.Value is not StatBuff frostBite)
                            {
                                continue;
                            }

                            sb.AppendLine($"- has Frostbite: {frostBite}");
                            sb.AppendLine($"  - Id: {frostBite.RowData.Id}");
                            sb.AppendLine($"  - Stack: {frostBite.Stack}");
                            sb.AppendLine($"  - CustomField(Power): {frostBite.CustomField}");
                            sb.AppendLine($"  - GroupId: {frostBite.BuffInfo.GroupId}");
                            sb.AppendLine($"  - Duration: {frostBite.BuffInfo.Duration}");
                        }
                    }

                    break;
                case TickDamage tickDamage:
                    sb.AppendLine($"OnTickDamage: {tickDamage.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {tickDamage.Character.RowData.Id}");
                    sb.AppendLine($"- SkillId: {tickDamage.SkillId}");
                    break;
                case WaveTurnEnd waveTurnEnd:
                    sb.AppendLine($"OnWaveTurnEnd: {waveTurnEnd.Character.Id} ({eventIndex}/{eventCount})");
                    sb.AppendLine($"- Id: {waveTurnEnd.Character.RowData.Id}");
                    break;
            }

            NcDebug.Log(sb.ToString(), "EventLog");
        }
    }
}
