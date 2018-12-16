using System;
using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.Game.CC
{
    public interface ICCBase
    {
        float Duration { get; } 
        float TickInterval { get; }
        bool Active { get; }
        float ElapsedTime { get; }
        int TotalTicks { get; }
        int ElapsedBeforeTicks { get; }
        int ElapsedAfterTicks { get; }
    }

    public class CCBase : MonoBehaviour, ICCBase
    {
        protected CharacterBase Owner => GetComponentInParent<CharacterBase>();
        public float Duration { get; private set; } 
        public float TickInterval { get; private set; }
        public bool Active { get; private set; } = false;
        public float ElapsedTime { get; private set; } = 0.0f;
        public int TotalTicks => Mathf.FloorToInt(ElapsedTime / TickInterval);
        public int ElapsedBeforeTicks => Math.Max(TotalTicks, _elapsedTicks + 1);
        public int ElapsedAfterTicks => _elapsedTicks;
        private int _elapsedTicks = 0;
        
        protected virtual bool DestroyOnEnd => true;

        public void Set(float duration, float tickInterval = 1.0f)
        {
            Duration = duration;
            TickInterval = tickInterval;
            Active = true;
            ElapsedTime = 0.0f;
            _elapsedTicks = 0;
            OnBegin();
            OnTickBefore();
        }

        private void FixedUpdate()
        {
            if (!Active) return;
            
            ElapsedTime += Time.deltaTime;
            if (ElapsedTime - _elapsedTicks * TickInterval >= TickInterval)
            {
                _elapsedTicks++;
                OnTickAfter();
                if (ElapsedTime >= Duration)
                {
                    OnEnd();
                    Active = false;
                    if (DestroyOnEnd)
                        Destroy(this);
                }
                else
                {
                    OnTickBefore();
                }
            }
        }

        protected virtual void OnBegin()
        {
        }

        protected virtual void OnTickBefore()
        {
        }

        protected virtual void OnTickAfter()
        {
        }

        protected virtual void OnEnd()
        {
        }
    }
}
