using UnityEngine;

namespace Nekoyume.Game
{
    public class AuraPrefabBase : MonoBehaviour
    {
        private Character.Character _owner;

        public Character.Character Owner
        {
            get => _owner;
            set
            {
                RemoveEventFromOwner();
                _owner = value;
                AddEventToOwner();
            }
        }

#region MonoBehaviour
        protected virtual void OnDisable()
        {
            RemoveEventFromOwner();
        }

        protected virtual void OnDestroy()
        {
            RemoveEventFromOwner();
        }
#endregion MonoBehaviour

        protected virtual void AddEventToOwner()
        {
            if (_owner == null)
                return;

            _owner.OnBuff += ProcessBuff;
            _owner.OnCustomEvent += ProcessCustomEvent;
            _owner.OnBuffEnd += ProcessBuffEnd;
        }

        protected virtual void RemoveEventFromOwner()
        {
            if (_owner == null)
                return;

            _owner.OnBuff -= ProcessBuff;
            _owner.OnCustomEvent -= ProcessCustomEvent;
            _owner.OnBuffEnd -= ProcessBuffEnd;
        }

        /// <summary>
        /// buff수행시 호출되는 이벤트. 연출 대기가 필요한 경우 Owner.BuffCastCoroutine에 코루틴을 추가해주어야 함
        /// </summary>
        /// <param name="buffId">발생된 버프 ID</param>
        protected virtual void ProcessBuff(int buffId)
        {

        }

        protected virtual void ProcessCustomEvent(int customEventId)
        {

        }

        protected virtual void ProcessBuffEnd(int buffId)
        {

        }
    }
}
