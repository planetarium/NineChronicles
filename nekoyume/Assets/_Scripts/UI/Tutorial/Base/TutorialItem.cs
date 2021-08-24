using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class TutorialItem : MonoBehaviour, ITutorialItem
    {
        [SerializeField] protected float predelay;
        public abstract void Play<T>(T data, System.Action callback) where T : ITutorialData;
        public abstract void Stop(System.Action callback);
        public abstract void Skip(System.Action callback);
    }
}
