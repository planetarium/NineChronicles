using System;

namespace Nekoyume.Model
{
    [Serializable]
    public class BattleLog
    {
        public LogType type;
        public CharacterBase character;
        public CharacterBase target;
        public int atk;
        public ResultType result;
        public int stage;
        public Guid characterId;
        public Guid targetId;

        public enum LogType
        {
            Attack,
            BattleResult,
            Dead,
            GetItem,
            LevelUp,
            Spawn,
            StartStage,
        }

        public enum ResultType
        {
            Win,
            Lose,
        }
    }
}
