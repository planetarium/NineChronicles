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
