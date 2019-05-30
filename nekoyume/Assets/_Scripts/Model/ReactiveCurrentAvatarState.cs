using System;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// 현재 선택된 AvatarState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveCurrentAvatarState
    {
        public static readonly ReactiveProperty<AvatarState> AvatarState = new ReactiveProperty<AvatarState>();
        
        public static void Initialize(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                return;
            }

            AvatarState.Value = avatarState;
        }
    }
}
