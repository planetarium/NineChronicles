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

        public enum LogType
        {
            Attack,
            BattleResult,
            Casting,
            Dead,
            GetItem,
            LevelUp,
            Spawn,
            StartStage,
            Walk,
        }

        public enum ResultType
        {
            Win,
            Lose,
        }

    }
}
