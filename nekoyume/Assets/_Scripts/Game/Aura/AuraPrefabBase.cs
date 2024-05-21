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
                if (_owner == value)
                    return;

                RemoveEventFromOwner();
                _owner = value;
                AddEventToOwner();
            }
        }

#region MonoBehaviour
        protected void OnDisable()
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
        }

        protected virtual void RemoveEventFromOwner()
        {
            if (_owner == null)
                return;

            _owner.OnBuff -= ProcessBuff;
            _owner.OnCustomEvent -= ProcessCustomEvent;
        }

        protected virtual void ProcessBuff(int buffId)
        {

        }

        protected virtual void ProcessCustomEvent(int customEventId)
        {

        }
    }
}
