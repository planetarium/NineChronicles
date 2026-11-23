using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nekoyume.Telemetry
{
    public class TelemetryQueue
    {
        private readonly int _maxEvents;
        private readonly LinkedList<TelemetryEvent> _items = new LinkedList<TelemetryEvent>();
        private readonly object _lock = new object();

        public TelemetryQueue(int maxEvents = 2000)
        {
            _maxEvents = maxEvents;
        }

        public int Count
        {
            get { lock (_lock) return _items.Count; }
        }

        public void Enqueue(TelemetryEvent e)
        {
            lock (_lock)
            {
                _items.AddLast(e);
                Trim();
            }
        }

        public List<TelemetryEvent> TakeBatch(int max = 500)
        {
            lock (_lock)
            {
                var list = new List<TelemetryEvent>();
                while (list.Count < max && _items.First != null)
                {
                    list.Add(_items.First.Value);
                    _items.RemoveFirst();
                }

                return list;
            }
        }

        public void Requeue(List<TelemetryEvent> events)
        {
            lock (_lock)
            {
                for (var i = events.Count - 1; i >= 0; i--)
                {
                    _items.AddFirst(events[i]);
                }

                Trim();
            }
        }

        public string ToJson()
        {
            lock (_lock)
            {
                return JsonConvert.SerializeObject(new List<TelemetryEvent>(_items));
            }
        }

        public void FromJson(string json)
        {
            lock (_lock)
            {
                _items.Clear();
                try
                {
                    var list = JsonConvert.DeserializeObject<List<TelemetryEvent>>(json);
                    if (list == null) return;
                    foreach (var e in list)
                    {
                        if (e != null) _items.AddLast(e);
                    }

                    Trim();
                }
                catch
                {
                    _items.Clear();
                }
            }
        }

        private void Trim()
        {
            while (_items.Count > _maxEvents)
            {
                _items.RemoveFirst();
            }
        }
    }
}
