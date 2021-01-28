using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class TutorialItem : MonoBehaviour, ITutorialItem
    {
        public abstract void Play<T>(T data, System.Action callback) where T : ITutorialData;
        public abstract void Stop();
    }
}
