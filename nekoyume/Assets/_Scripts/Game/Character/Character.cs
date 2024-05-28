using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Character;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class Character : MonoBehaviour
    {
        public Guid Id {get; protected set; }
        public SizeType SizeType { get; protected set; }
        public CharacterAnimator Animator { get; protected set; }
        protected virtual Vector3 HUDOffset => Animator.GetHUDPosition();
        protected Vector3 HealOffset => Animator.HealPosition;
        protected bool AttackEndCalled { get; set; }

        protected System.Action ActionPoint;

        public readonly Dictionary<int, IEnumerator> BuffRemoveCoroutine = new();
        public readonly Dictionary<int, Func<BuffCastingVFX, IEnumerator>> BuffCastCoroutine = new();

        protected void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "AttackStart":
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "AttackPoint":
                case "attackPoint":
                    AttackEndCalled = true;
                    ActionPoint?.Invoke();
                    ActionPoint = null;
                    break;
                case "Footstep":
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }

        protected virtual void Update()
        {
            UpdateColor();
        }

        protected virtual void OnDisable()
        {
            SetDefaultColor();
            _expiredColorKeys.Clear();
            _colorPq.Clear();
        }

        // TODO: 이 클래스에 존재해야할 느낌은 아니지만, 중복 구현을 피하기 위해 일단 여기에 둠
#region Temp
#region Event
        public Action<int> OnBuff;
        public Action<int> OnBuffEnd;
        public Action<int> OnCustomEvent;
#endregion Event

        public bool IsFlipped { get; set; }

        /// <summary>
        /// Stage.CoCustomEvent를 통해 실행된 이벤트를 받아 처리하기 위해 생성
        /// </summary>
        /// <param name="customEventId">이벤트 ID</param>
        public void CustomEvent(int customEventId)
        {
            OnCustomEvent?.Invoke(customEventId);
        }

        protected async UniTask CastingOnceAsync()
        {
            Animator.Cast();
            await UniTask.Delay(TimeSpan.FromSeconds(Game.DefaultSkillDelay));
            Animator.Idle();
        }
#endregion Temp

// TODO: 별도 클래스로 분리?
#region SpineColor
        private readonly Priority_Queue.SimplePriorityQueue<SpineColorSetting, int> _colorPq = new();

        private readonly HashSet<SpineColorKey> _expiredColorKeys = new();

        private int _currentColorHash;

        private void UpdateColor()
        {
            if (_colorPq.Count == 0)
            {
                return;
            }

            ExpireColors();

            while (_colorPq.Count > 0)
            {
                var setting = _colorPq.First;
                if (setting.IsExpired)
                {
                    _colorPq.Dequeue();
                    if (_colorPq.Count == 0)
                    {
                        SetDefaultColor();
                    }
                    continue;
                }

                if (setting.GetHashCode() == _currentColorHash)
                {
                    break;
                }

                _currentColorHash = setting.GetHashCode();
                setting.SetColor(this);
                return;
            }
        }

        private void ExpireColors()
        {
            foreach (var colorSetting in _colorPq)
            {
                foreach (var expiredColorKey in _expiredColorKeys)
                {
                    colorSetting.ExpireByKey(expiredColorKey);
                }
                colorSetting.UpdateDuration(Time.deltaTime);

                if (colorSetting.IsExpired)
                {
                    colorSetting.Expire();
                }
            }
            _expiredColorKeys.Clear();
        }

        public virtual void SetSpineColor(Color color, int propertyID = -1)
        {
        }

        private void SetDefaultColor()
        {
            SetSpineColor(Color.white, SpineColorSetting.ColorPropertyId);
            SetSpineColor(Color.black, SpineColorSetting.BlackPropertyId);
            _currentColorHash = 0;
        }

        public void AddHitColor()
        {
            var color = new Color(1, 0.6651f, 0.65566f, 1f);
            var black = new Color(0.2452f, 0.091f, 0.091f, 1f);
            _colorPq.Enqueue(new SpineColorSetting(color, black, true, 0.3f), (int)SpineColorPriority.Hit);
        }

        public void AddFrostbiteColor()
        {
            // color: 31FFF4
            // black: 0F3069
            var color = new Color(0.1921f, 1f, 0.9568f, 1f);
            var black = new Color(0.0588f, 0.1882f, 0.4117f, 1f);

            //
            foreach (var colorSetting in _colorPq)
            {
                if (colorSetting.Key == SpineColorKey.FrostBite)
                {
                    return;
                }
            }

            _colorPq.Enqueue(new SpineColorSetting(color, black, key: SpineColorKey.FrostBite), (int)SpineColorPriority.Frostbite);
        }

        public void RemoveFrostbiteColor()
        {
            _expiredColorKeys.Add(SpineColorKey.FrostBite);
        }

        public void AddSpineColor(Color color, bool hasDuration = false, float duration = 0f, SpineColorKey key = SpineColorKey.None)
        {
            _colorPq.Enqueue(new SpineColorSetting(color, hasDuration, duration, key), 0);
        }

        public void AddSpineColor(Color color, Color black, bool hasDuration = false, float duration = 0f, SpineColorKey key = SpineColorKey.None)
        {
            _colorPq.Enqueue(new SpineColorSetting(color, black, hasDuration, duration, key), 0);
        }
#endregion SpineColor
    }
}
