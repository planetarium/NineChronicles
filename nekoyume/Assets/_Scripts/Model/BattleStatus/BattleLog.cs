using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model
{
    [Serializable]
    public class BattleLog : IEnumerable<EventBase>
    {
        public List<EventBase> events = new List<EventBase>();
        public int Count => events.Count;

        public void Add(EventBase e)
        {
            events.Add(e);
        }

        public StartStage StartStage()
        {
            var startStage = events.Find(e => e is StartStage);
            return (StartStage) startStage;
        }

        public List<Spawn> MonsterSpawns()
        {
            return events.OfType<Spawn>().Where(e => e.character is Monster).ToList();
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
