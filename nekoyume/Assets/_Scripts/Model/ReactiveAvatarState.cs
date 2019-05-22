using Nekoyume.Action;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// AvatarState가 포함하는 값의 변화를 ActionBase.EveryRender<T>()를 통해 감지하고, 동기화한다.
    /// 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// 실 구현은 별도의 PR로 진행한다.
    /// </summary>
    public static class ReactiveAvatarState
    {
        static ReactiveAvatarState()
        {
            Subscribes();
        }

        public static void Initialize(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                return;
            }
            
            // 기본 값을 초기화 한다.
        }

        private static void Subscribes()
        {
            // ActionBase.EveryRender<T>() 를 통해 필요한 동기화를 진행한다.
        }
    }
}
