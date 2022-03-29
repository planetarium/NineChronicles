using mixpanel;
using PackageExtensions.Mixpanel;
using UnityEngine;

namespace Nekoyume
{
    public class Analyzer
    {
        public static Analyzer Instance => Game.Game.instance.Analyzer;

        private readonly MixpanelValueFactory _mixpanelValueFactory;

        public Analyzer(
            string uniqueId = "none",
            string rpcServerHost = null)
        {
#if UNITY_EDITOR
            Debug.Log("Analyzer does not track in editor mode");
            return;
#endif

            _mixpanelValueFactory = new MixpanelValueFactory(rpcServerHost);

            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Identify(uniqueId);
            Mixpanel.Init();

            Debug.Log($"Analyzer initialized: {uniqueId}");
        }

        public void Track(string eventName, params (string key, string value)[] properties)
        {
#if UNITY_EDITOR
            return;
#endif
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
#if UNITY_EDITOR
            return;
#endif
            value = _mixpanelValueFactory.UpdateValue(value);
            Mixpanel.Track(eventName, value);
        }

        public void Flush()
        {
#if UNITY_EDITOR
            return;
#endif
            Mixpanel.Flush();
        }
    }
}
