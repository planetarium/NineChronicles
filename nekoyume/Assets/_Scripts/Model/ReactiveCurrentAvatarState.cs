using System;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// AvatarState가 포함하는 값의 변화를 ActionBase.EveryRender<T>()를 통해 감지하고, 동기화한다.
    /// 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// 실 구현은 별도의 PR로 진행한다.
    /// </summary>
    public static class ReactiveCurrentAvatarState
    {
        public static readonly ReactiveProperty<AvatarState> AvatarState = new ReactiveProperty<AvatarState>();
        
        public static void Initialize(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                throw new ArgumentNullException(nameof(avatarState));
            }

            AvatarState.Value = avatarState;
        }
    }
}
