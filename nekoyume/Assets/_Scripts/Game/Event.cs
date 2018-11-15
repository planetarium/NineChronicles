using UnityEngine.Events;

namespace Nekoyume.Game
{
    static public class Event
    {
        [System.Serializable]
        public class StageEnter : UnityEvent<Model.Avatar> {}
        static public StageEnter OnStageEnter = new StageEnter();
        static public UnityEvent OnStageStart = new UnityEvent();
    }
}
