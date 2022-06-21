using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaLog : IEnumerable<ArenaEventBase>
    {
        public Guid Id = Guid.NewGuid();

        public enum ArenaResult
        {
            Win,
            Lose,
        }

        public List<ArenaEventBase> Events = new List<ArenaEventBase>();

        public ArenaResult Result = ArenaResult.Lose;
        public int Score;

        public void Add(ArenaEventBase e)
        {
            Events.Add(e);
        }

        public IEnumerator<ArenaEventBase> GetEnumerator()
        {
            return Events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
