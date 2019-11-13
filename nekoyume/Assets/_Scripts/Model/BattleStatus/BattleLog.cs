using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model
{
    [Serializable]
    public class BattleLog : IEnumerable<EventBase>
    {
        public Guid id = Guid.NewGuid();
        public enum Result
        {
            Win,
            Lose
        }

        public List<EventBase> events = new List<EventBase>();
        public int Count => events.Count;
        public int worldId;
        public int stageId;
        public Result result;

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
