using mixpanel;
using PackageExtensions.Mixpanel;
using UnityEngine;

namespace Nekoyume
{
    public class Analyzer
    {
        public static Analyzer Instance => Game.Game.instance.Analyzer;

        private readonly MixpanelValueFactory _mixpanelValueFactory;

        public Analyzer()
        {
            _mixpanelValueFactory = new MixpanelValueFactory();
        }

        public Analyzer Initialize(string uniqueId = "non-unique-id")
        {
#if UNITY_EDITOR
            Debug.Log("Analyzer does not track in editor mode");
#else
            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Identify(uniqueId);
            Mixpanel.Init();
#endif
            Debug.Log($"Analyzer initialized: {uniqueId}");
            return this;
        }

        public void Track(string eventName, params (string key, string value)[] properties)
        {
            if (properties.Length == 0)
            {
                Mixpanel.Track(eventName);
                return;
            }

            var value = _mixpanelValueFactory.GetValue(properties);
            Mixpanel.Track(eventName, value);
        }

        public void Track(string eventName, Value value)
        {
            value = _mixpanelValueFactory.UpdateValue(value);
            Mixpanel.Track(eventName, value);
        }

        public void Flush()
        {
            Mixpanel.Flush();
        }
    }
}
