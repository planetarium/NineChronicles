using System;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public interface ICharacterAnimator : IDisposable
    {
        /// <summary>
        /// 캐릭터의 루트 게임 오브젝트.
        /// </summary>
        CharacterBase root { get; }
        
        /// <summary>
        /// 컨트롤 하려는 애니메이터가 붙어 있는 게임 오브젝트.
        /// </summary>
        GameObject target { get; }
        
        Subject<string> onEvent { get; }

        void ResetTarget(GameObject value);
        bool AnimatorValidation();
        Vector3 GetHUDPosition();
        void SetTimeScale(float value);
        
        #region Animation

        void Appear();
        void Idle();
        void Run();
        void StopRun();
        void Attack();
        void Hit();
        void Die();
        void Disappear();

        #endregion
    }
}
