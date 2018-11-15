using UnityEngine.Events;

namespace Nekoyume.Game
{
    static public class Event
    {
        [System.Serializable]
        public class UpdateAvatar : UnityEvent<Model.Avatar> {}
        static public UpdateAvatar OnUpdateAvatar = new UpdateAvatar();
        static public UnityEvent OnRoomEnter = new UnityEvent();
        static public UnityEvent OnStageEnter = new UnityEvent();
        static public UnityEvent OnStageStart = new UnityEvent();
    }
}
