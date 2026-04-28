using System;
using System.Collections.Generic;
using System.Text;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.BattleStatus.AdventureBoss;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using ArenaBuff = Nekoyume.Model.BattleStatus.Arena.ArenaBuff;
using ArenaSkillType = Nekoyume.Model.BattleStatus.Arena.ArenaSkill;
using Buff = Nekoyume.Model.BattleStatus.Buff;
using Skill = Nekoyume.Model.BattleStatus.Skill;

namespace Nekoyume
{
    public static class ActionBaseExtensions
    {
        public static bool EnableBattleLog = false;

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
            if (!EnableBattleLog) return;

            var sb = new StringBuilder();

            switch (e)
            {
                case Dead dead:
                    sb.AppendLine($"OnDead: {dead.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {dead.Character?.RowData?.Id}");
                    break;
                case SpawnPlayer spawnPlayer:
                    sb.AppendLine($"OnSpawnPlayer: {spawnPlayer.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {spawnPlayer.Character?.RowData?.Id}");
                    LogCharacterStats(sb, spawnPlayer.Character);
                    break;
                case SpawnWave spawnWave:
                    sb.AppendLine($"OnSpawnWave: {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- WaveNumber: {spawnWave.WaveNumber}, WaveTurn: {spawnWave.WaveTurn}, HasBoss: {spawnWave.HasBoss}");
                    foreach (var enemy in spawnWave.Enemies)
                    {
                        sb.AppendLine($"- Enemy: {enemy.RowData?.Id ?? enemy.CharacterId} (Lv.{enemy.Level})");
                    }

                    break;
                case Breakthrough breakthrough:
                    sb.AppendLine($"OnBreakthrough: {breakthrough.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- FloorId: {breakthrough.FloorId}");
                    foreach (var monster in breakthrough.Monsters)
                    {
                        sb.AppendLine($"- Monster: {monster.CharacterId} (Lv.{monster.Level}, Count: {monster.Count})");
                    }

                    break;
                case Buff buff:
                    sb.AppendLine($"OnBuff: {buff.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {buff.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {buff.SkillId}");
                    LogBuffInfos(sb, buff.SkillInfos);
                    break;
                case StageBuff stageBuff:
                    sb.AppendLine($"OnStageBuff: {stageBuff.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {stageBuff.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {stageBuff.SkillId}");
                    LogSkillInfos(sb, stageBuff.SkillInfos);
                    LogBuffInfos(sb, stageBuff.BuffInfos);
                    break;
                case BuffRemovalAttack buffRemovalAttack:
                    sb.AppendLine($"OnBuffRemovalAttack: {buffRemovalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {buffRemovalAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {buffRemovalAttack.SkillId}");
                    LogSkillInfos(sb, buffRemovalAttack.SkillInfos);
                    LogBuffInfos(sb, buffRemovalAttack.BuffInfos);
                    break;
                case FullBuffRemovalAttack fullBuffRemovalAttack:
                    sb.AppendLine($"OnFullBuffRemovalAttack: {fullBuffRemovalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {fullBuffRemovalAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {fullBuffRemovalAttack.SkillId}");
                    LogSkillInfos(sb, fullBuffRemovalAttack.SkillInfos);
                    LogBuffInfos(sb, fullBuffRemovalAttack.BuffInfos);
                    break;
                case ShatterStrike shatterStrike:
                    sb.AppendLine($"OnShatterStrike: {shatterStrike.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {shatterStrike.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {shatterStrike.SkillId}");
                    LogSkillInfos(sb, shatterStrike.SkillInfos);
                    LogBuffInfos(sb, shatterStrike.BuffInfos);
                    break;
                case BlowAttack blowAttack:
                    sb.AppendLine($"OnBlowAttack: {blowAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {blowAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {blowAttack.SkillId}");
                    LogSkillInfos(sb, blowAttack.SkillInfos);
                    LogBuffInfos(sb, blowAttack.BuffInfos);
                    break;
                case AreaAttack areaAttack:
                    sb.AppendLine($"OnAreaAttack: {areaAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {areaAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {areaAttack.SkillId}");
                    LogSkillInfos(sb, areaAttack.SkillInfos);
                    LogBuffInfos(sb, areaAttack.BuffInfos);
                    break;
                case DoubleAttackWithCombo doubleAttackWithCombo:
                    sb.AppendLine($"OnDoubleAttackWithCombo: {doubleAttackWithCombo.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {doubleAttackWithCombo.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {doubleAttackWithCombo.SkillId}");
                    LogSkillInfos(sb, doubleAttackWithCombo.SkillInfos);
                    LogBuffInfos(sb, doubleAttackWithCombo.BuffInfos);
                    break;
                case DoubleAttack doubleAttack:
                    sb.AppendLine($"OnDoubleAttack: {doubleAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {doubleAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {doubleAttack.SkillId}");
                    LogSkillInfos(sb, doubleAttack.SkillInfos);
                    LogBuffInfos(sb, doubleAttack.BuffInfos);
                    break;
                case HealSkill healSkill:
                    sb.AppendLine($"OnHealSkill: {healSkill.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {healSkill.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {healSkill.SkillId}");
                    LogSkillInfos(sb, healSkill.SkillInfos);
                    LogBuffInfos(sb, healSkill.BuffInfos);
                    break;
                case NormalAttack normalAttack:
                    sb.AppendLine($"OnNormalAttack: {normalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {normalAttack.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {normalAttack.SkillId}");
                    LogSkillInfos(sb, normalAttack.SkillInfos);
                    LogBuffInfos(sb, normalAttack.BuffInfos);
                    break;
                case RemoveBuffs removeBuffs:
                    sb.AppendLine($"OnRemoveBuffs: {removeBuffs.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    break;
                case Tick tick:
                    sb.AppendLine($"OnTick: {tick.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {tick.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {tick.SkillId}");
                    LogSkillInfos(sb, tick.SkillInfos);
                    if (AuraIceShield.IsFrostBiteBuff(tick.SkillId) && tick.Character?.Buffs != null)
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
                    sb.AppendLine($"OnTickDamage: {tickDamage.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {tickDamage.Character?.RowData?.Id}");
                    sb.AppendLine($"- SkillId: {tickDamage.SkillId}");
                    LogSkillInfos(sb, tickDamage.SkillInfos);
                    break;
                case WaveTurnEnd waveTurnEnd:
                    sb.AppendLine($"OnWaveTurnEnd: {waveTurnEnd.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- Id: {waveTurnEnd.Character?.RowData?.Id}");
                    sb.AppendLine($"- TurnNumber: {waveTurnEnd.TurnNumber}, WaveTurn: {waveTurnEnd.WaveTurn}");
                    break;
            }

            NcDebug.Log(sb.ToString(), "EventLog");
        }

        public static void LogEvent(this ArenaEventBase e, int eventIndex, int eventCount)
        {
#if !DEBUG_USE
            return;
#endif
            if (!EnableBattleLog) return;

            var sb = new StringBuilder();

            switch (e)
            {
                case ArenaDead dead:
                    sb.AppendLine($"[Arena]OnDead: {dead.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {dead.Character?.CharacterId}");
                    break;
                case ArenaSpawnCharacter spawnCharacter:
                    sb.AppendLine($"[Arena]OnSpawnCharacter: {spawnCharacter.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {spawnCharacter.Character?.CharacterId}");
                    LogArenaCharacterStats(sb, spawnCharacter.Character);
                    break;
                case ArenaBuff buff:
                    sb.AppendLine($"[Arena]OnBuff: {buff.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {buff.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {buff.SkillId}");
                    LogArenaBuffInfos(sb, buff.SkillInfos);
                    break;
                case ArenaNormalAttack normalAttack:
                    sb.AppendLine($"[Arena]OnNormalAttack: {normalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {normalAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {normalAttack.SkillId}");
                    LogArenaSkillInfos(sb, normalAttack.SkillInfos);
                    LogArenaBuffInfos(sb, normalAttack.BuffInfos);
                    break;
                case ArenaBlowAttack blowAttack:
                    sb.AppendLine($"[Arena]OnBlowAttack: {blowAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {blowAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {blowAttack.SkillId}");
                    LogArenaSkillInfos(sb, blowAttack.SkillInfos);
                    LogArenaBuffInfos(sb, blowAttack.BuffInfos);
                    break;
                case ArenaDoubleAttackWithCombo doubleAttackWithCombo:
                    sb.AppendLine($"[Arena]OnDoubleAttackWithCombo: {doubleAttackWithCombo.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {doubleAttackWithCombo.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {doubleAttackWithCombo.SkillId}");
                    LogArenaSkillInfos(sb, doubleAttackWithCombo.SkillInfos);
                    LogArenaBuffInfos(sb, doubleAttackWithCombo.BuffInfos);
                    break;
                case ArenaDoubleAttack doubleAttack:
                    sb.AppendLine($"[Arena]OnDoubleAttack: {doubleAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {doubleAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {doubleAttack.SkillId}");
                    LogArenaSkillInfos(sb, doubleAttack.SkillInfos);
                    LogArenaBuffInfos(sb, doubleAttack.BuffInfos);
                    break;
                case ArenaAreaAttack areaAttack:
                    sb.AppendLine($"[Arena]OnAreaAttack: {areaAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {areaAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {areaAttack.SkillId}");
                    LogArenaSkillInfos(sb, areaAttack.SkillInfos);
                    LogArenaBuffInfos(sb, areaAttack.BuffInfos);
                    break;
                case ArenaBuffRemovalAttack buffRemovalAttack:
                    sb.AppendLine($"[Arena]OnBuffRemovalAttack: {buffRemovalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {buffRemovalAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {buffRemovalAttack.SkillId}");
                    LogArenaSkillInfos(sb, buffRemovalAttack.SkillInfos);
                    LogArenaBuffInfos(sb, buffRemovalAttack.BuffInfos);
                    break;
                case ArenaFullBuffRemovalAttack fullBuffRemovalAttack:
                    sb.AppendLine($"[Arena]OnFullBuffRemovalAttack: {fullBuffRemovalAttack.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {fullBuffRemovalAttack.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {fullBuffRemovalAttack.SkillId}");
                    LogArenaSkillInfos(sb, fullBuffRemovalAttack.SkillInfos);
                    LogArenaBuffInfos(sb, fullBuffRemovalAttack.BuffInfos);
                    break;
                case ArenaShatterStrike shatterStrike:
                    sb.AppendLine($"[Arena]OnShatterStrike: {shatterStrike.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {shatterStrike.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {shatterStrike.SkillId}");
                    LogArenaSkillInfos(sb, shatterStrike.SkillInfos);
                    LogArenaBuffInfos(sb, shatterStrike.BuffInfos);
                    break;
                case ArenaHeal heal:
                    sb.AppendLine($"[Arena]OnHeal: {heal.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {heal.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {heal.SkillId}");
                    LogArenaSkillInfos(sb, heal.SkillInfos);
                    LogArenaBuffInfos(sb, heal.BuffInfos);
                    break;
                case ArenaTick tick:
                    sb.AppendLine($"[Arena]OnTick: {tick.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {tick.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {tick.SkillId}");
                    LogArenaSkillInfos(sb, tick.SkillInfos);
                    break;
                case ArenaTickDamage tickDamage:
                    sb.AppendLine($"[Arena]OnTickDamage: {tickDamage.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- CharacterId: {tickDamage.Character?.CharacterId}");
                    sb.AppendLine($"- SkillId: {tickDamage.SkillId}");
                    LogArenaSkillInfos(sb, tickDamage.SkillInfos);
                    break;
                case ArenaRemoveBuffs removeBuffs:
                    sb.AppendLine($"[Arena]OnRemoveBuffs: {removeBuffs.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    break;
                case ArenaTurnEnd turnEnd:
                    sb.AppendLine($"[Arena]OnTurnEnd: {turnEnd.Character?.Id} {GetProgressText(eventIndex, eventCount)}");
                    sb.AppendLine($"- TurnNumber: {turnEnd.TurnNumber}");
                    break;
            }

            NcDebug.Log(sb.ToString(), "ArenaEventLog");
        }

        private static string GetProgressText(int eventIndex, int eventCount)
        {
            return $"(Event Count: {eventIndex}/{eventCount})";
        }

        private static void LogCharacterStats(StringBuilder sb, CharacterBase character)
        {
            if (character == null) return;
            sb.AppendLine($"- HP: {character.CurrentHP}/{character.HP}, ATK: {character.ATK}, DEF: {character.DEF}");
            sb.AppendLine($"- CRI: {character.CRI}, HIT: {character.HIT}, SPD: {character.SPD}");
            sb.AppendLine($"- DRV: {character.DRV}, DRR: {character.DRR}, CDMG: {character.CDMG}");
        }

        private static void LogArenaCharacterStats(StringBuilder sb, ArenaCharacter character)
        {
            if (character == null) return;
            sb.AppendLine($"- HP: {character.CurrentHP}/{character.HP}, ATK: {character.ATK}, DEF: {character.DEF}");
            sb.AppendLine($"- CRI: {character.CRI}, HIT: {character.HIT}, SPD: {character.SPD}");
            sb.AppendLine($"- DRV: {character.DRV}, DRR: {character.DRR}, CDMG: {character.CDMG}");
        }

        private static void LogSkillInfos(StringBuilder sb, IEnumerable<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos == null)
            {
                return;
            }

            foreach (var info in skillInfos)
            {
                sb.AppendLine($"- SkillInfo: target={info.Target?.Id}, effect={info.Effect}, critical={info.Critical}");
                sb.AppendLine($"  - category: {info.SkillCategory}, elemental: {info.ElementalType}, targetType: {info.SkillTargetType}");
            }
        }

        private static void LogBuffInfos(StringBuilder sb, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            if (buffInfos == null)
            {
                return;
            }

            foreach (var info in buffInfos)
            {
                if (info.Buff == null)
                {
                    continue;
                }

                sb.AppendLine($"- BuffInfo: target={info.Target?.Id}, affected={info.Affected}");
                sb.AppendLine($"  - buffId: {info.Buff.BuffInfo.Id} (GroupId: {info.Buff.BuffInfo.GroupId}, Duration: {info.Buff.BuffInfo.Duration})");

                if (info.Buff is StatBuff statBuff)
                {
                    var modifier = statBuff.GetModifier();
                    sb.AppendLine($"  - StatBuff: {modifier.StatType} {modifier}");
                    if (info.Target != null)
                    {
                        sb.AppendLine($"  - Target post-buff {modifier.StatType}: {GetStatValue(info.Target, modifier.StatType)}");
                    }
                }
                else if (info.Buff is ActionBuff actionBuff)
                {
                    sb.AppendLine($"  - ActionBuff: {actionBuff.GetType().Name}");
                }
            }
        }

        private static void LogArenaSkillInfos(StringBuilder sb, IEnumerable<ArenaSkillType.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos == null)
            {
                return;
            }

            foreach (var info in skillInfos)
            {
                sb.AppendLine($"- SkillInfo: target={info.Target?.Id}, effect={info.Effect}, critical={info.Critical}");
                sb.AppendLine($"  - category: {info.SkillCategory}, elemental: {info.ElementalType}, targetType: {info.SkillTargetType}");
            }
        }

        private static void LogArenaBuffInfos(StringBuilder sb, IEnumerable<ArenaSkillType.ArenaSkillInfo> buffInfos)
        {
            if (buffInfos == null)
            {
                return;
            }

            foreach (var info in buffInfos)
            {
                if (info.Buff == null)
                {
                    continue;
                }

                sb.AppendLine($"- BuffInfo: target={info.Target?.Id}, affected={info.Affected}");
                sb.AppendLine($"  - buffId: {info.Buff.BuffInfo.Id} (GroupId: {info.Buff.BuffInfo.GroupId}, Duration: {info.Buff.BuffInfo.Duration})");

                if (info.Buff is StatBuff statBuff)
                {
                    var modifier = statBuff.GetModifier();
                    sb.AppendLine($"  - StatBuff: {modifier.StatType} {modifier}");
                    if (info.Target != null)
                    {
                        sb.AppendLine($"  - Target post-buff {modifier.StatType}: {GetArenaStatValue(info.Target, modifier.StatType)}");
                    }
                }
                else if (info.Buff is ActionBuff actionBuff)
                {
                    sb.AppendLine($"  - ActionBuff: {actionBuff.GetType().Name}");
                }
            }
        }

        private static long GetStatValue(CharacterBase character, StatType statType)
        {
            return statType switch
            {
                StatType.HP => character.HP,
                StatType.ATK => character.ATK,
                StatType.DEF => character.DEF,
                StatType.CRI => character.CRI,
                StatType.HIT => character.HIT,
                StatType.SPD => character.SPD,
                StatType.DRV => character.DRV,
                StatType.DRR => character.DRR,
                StatType.CDMG => character.CDMG,
                StatType.ArmorPenetration => character.ArmorPenetration,
                StatType.Thorn => character.Thorn,
                _ => 0,
            };
        }

        private static long GetArenaStatValue(ArenaCharacter character, StatType statType)
        {
            return statType switch
            {
                StatType.HP => character.HP,
                StatType.ATK => character.ATK,
                StatType.DEF => character.DEF,
                StatType.CRI => character.CRI,
                StatType.HIT => character.HIT,
                StatType.SPD => character.SPD,
                StatType.DRV => character.DRV,
                StatType.DRR => character.DRR,
                StatType.CDMG => character.CDMG,
                StatType.ArmorPenetration => character.ArmorPenetration,
                StatType.Thorn => character.Thorn,
                _ => 0,
            };
        }
    }
}
