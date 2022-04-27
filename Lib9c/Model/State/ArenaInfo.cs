using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    public class ArenaInfo : IState
    {
        public class Record : IState
        {
            public int Win;
            public int Lose;
            public int Draw;

            public Record()
            {
            }

            public Record(Dictionary serialized)
            {
                Win = serialized.GetInteger("win");
                Lose = serialized.GetInteger("lose");
                Draw = serialized.GetInteger("draw");
            }

            public IValue Serialize() =>
                Dictionary.Empty
                    .Add("win", Win.Serialize())
                    .Add("lose", Lose.Serialize())
                    .Add("draw", Draw.Serialize());
        }

        public readonly Address AvatarAddress;
        public readonly Record ArenaRecord;
        public int Score { get; private set; }
        public int DailyChallengeCount { get; private set; }
        public bool Active { get; private set; }

        public readonly Address AgentAddress;

        public readonly string AvatarName;

        [Obsolete("Not used anymore since v100070")]
        public bool Receive;

        [Obsolete("Not used anymore since v100070")]
        public int Level;

        [Obsolete("Not used anymore since v100070")]
        public int CombatPoint;

        [Obsolete("Not used anymore since v100070")]
        public int ArmorId;

        public ArenaInfo(AvatarState avatarState, CharacterSheet characterSheet, bool active)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            AvatarName = avatarState.NameWithHash;
            ArenaRecord = new Record();
            Level = avatarState.level;
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            CombatPoint = CPHelper.GetCP(avatarState, characterSheet);
            Active = active;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Score = GameConfig.ArenaScoreDefault;
        }

        public ArenaInfo(AvatarState avatarState, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet, bool active)
            : this(avatarState, characterSheet, active)
        {
            CombatPoint = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
        }

        public ArenaInfo(Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            AvatarName = serialized.GetString("avatarName");
            ArenaRecord = serialized.ContainsKey((IKey)(Text)"arenaRecord")
                ? new Record((Dictionary)serialized["arenaRecord"])
                : new Record();
            Level = serialized.GetInteger("level");
            ArmorId = serialized.GetInteger("armorId");
            CombatPoint = serialized.GetInteger("combatPoint");
            Active = IsActive(serialized);
            DailyChallengeCount = serialized.GetInteger("dailyChallengeCount");
            Score = serialized.GetInteger("score");
            Receive = serialized["receive"].ToBoolean();
        }

        public ArenaInfo(ArenaInfo prevInfo)
        {
            AvatarAddress = prevInfo.AvatarAddress;
            AgentAddress = prevInfo.AgentAddress;
            ArmorId = prevInfo.ArmorId;
            Level = prevInfo.Level;
            AvatarName = prevInfo.AvatarName;
            CombatPoint = prevInfo.CombatPoint;
            Score = 1000;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Active = false;
            ArenaRecord = new Record();
        }
        
        public ArenaInfo Clone() => new ArenaInfo(this)
        {
            Score = Score,
            DailyChallengeCount = DailyChallengeCount,
            Active = Active,
            Receive = Receive,
            ArenaRecord =
            {
                Win = ArenaRecord.Win,
                Lose = ArenaRecord.Lose,
                Draw = ArenaRecord.Draw
            }
        };

        public IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"avatarAddress"] = AvatarAddress.Serialize(),
                [(Text)"agentAddress"] = AgentAddress.Serialize(),
                [(Text)"avatarName"] = AvatarName.Serialize(),
                [(Text)"arenaRecord"] = ArenaRecord.Serialize(),
                [(Text)"level"] = Level.Serialize(),
                [(Text)"armorId"] = ArmorId.Serialize(),
                [(Text)"combatPoint"] = CombatPoint.Serialize(),
                [(Text)"active"] = Active.Serialize(),
                [(Text)"dailyChallengeCount"] = DailyChallengeCount.Serialize(),
                [(Text)"score"] = Score.Serialize(),
                [(Text)"receive"] = Receive.Serialize(),
            });

        #region Obsoleted `Update()` functions

        [Obsolete("Use Update()")]
        public void UpdateV1(AvatarState state, CharacterSheet characterSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCP(state, characterSheet);
        }

        [Obsolete("Use Update()")]
        public void UpdateV2(AvatarState state, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCPV2(state, characterSheet, costumeStatSheet);
        }

        [Obsolete("Use Update()")]
        public int UpdateV3(AvatarState avatarState, ArenaInfo enemyInfo, BattleLog.Result result)
        {
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var score = ArenaScoreHelper.GetScoreV1(Score, enemyInfo.Score, result);
            var calculated = Score + score;
            var current = Score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
            return Score - current;
        }

        [Obsolete("Use Update()")]
        public int UpdateV4(ArenaInfo enemyInfo, BattleLog.Result result)
        {
            DailyChallengeCount--;
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var earnedScore = ArenaScoreHelper.GetScoreV2(Score, enemyInfo.Score, result);
            Score = Math.Max(1000, Score + earnedScore);
            return earnedScore;
        }

        [Obsolete("Use Update()")]
        public int UpdateV5(ArenaInfo enemyInfo, BattleLog.Result result)
        {
            DailyChallengeCount--;
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var score =
                ArenaScoreHelper.GetScoreV3(Score, enemyInfo.Score, result);
            Score = Math.Max(1000, Score + score.challengerScore);
            enemyInfo.Score = Math.Max(1000, enemyInfo.Score + score.defenderScore);
            return score.challengerScore;
        }

        #endregion

        public int Update(
            ArenaInfo enemyInfo,
            BattleLog.Result result,
            Func<int, int, BattleLog.Result, (int challengerScore, int defenderScore)> scoreGetter)
        {
            DailyChallengeCount--;
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        
            var (challengerScore, defenderScore) = scoreGetter(Score, enemyInfo.Score, result);
            Score = Math.Max(1000, Score + challengerScore);
            enemyInfo.Score = Math.Max(1000, enemyInfo.Score + defenderScore);
            return challengerScore;
        }

        public void Activate()
        {
            Active = true;
        }

        public void ResetCount()
        {
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
        }

        public int GetRewardCount()
        {
            if (Score >= 1800)
            {
                return 6;
            }

            if (Score >= 1400)
            {
                return 5;
            }

            if (Score >= 1200)
            {
                return 4;
            }

            if (Score >= 1100)
            {
                return 3;
            }

            if (Score >= 1001)
            {
                return 2;
            }

            return 1;
        }

        public static bool IsActive(Dictionary serialized) =>
            serialized.GetBoolean("active");
    }
}
