using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class BattleLog : IEnumerable<EventBase>
    {
        public Guid id = Guid.NewGuid();
        public enum Result
        {
            Win,
            Lose,
            TimeOver,
        }

        public List<EventBase> events = new List<EventBase>();
        public int Count => events.Count;
        public int worldId;
        public int stageId;
        public int waveCount;
        public Result result = Result.Lose;
        public int score;
        public int diffScore;
        public int clearedWaveNumber;
        public bool newlyCleared;
        public bool IsClear => result == Result.Win && clearedWaveNumber == waveCount;

        public void Add(EventBase e)
        {
            events.Add(e);
        }

        public IEnumerator<EventBase> GetEnumerator()
        {
            return events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
