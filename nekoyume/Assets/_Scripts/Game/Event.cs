using UnityEngine.Events;

namespace Nekoyume.Game
{
    public static class Event
    {
        [System.Serializable]
        public class UpdateAvatar : UnityEvent<Model.Avatar> {}
        public static readonly UpdateAvatar OnUpdateAvatar = new UpdateAvatar();
        public static readonly UnityEvent OnRoomEnter = new UnityEvent();
        public static readonly UnityEvent OnStageEnter = new UnityEvent();
        public static readonly UnityEvent OnStageStart = new UnityEvent();
    }
}
