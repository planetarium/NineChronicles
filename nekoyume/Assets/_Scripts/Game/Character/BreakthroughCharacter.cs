using System;
using System.Collections;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using UniRx;
using UnityEngine;
using DG.Tweening;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class BreakthroughCharacter : MonoBehaviour
    {
        public bool AttackEndCalled { get; set; }
        public CharacterAnimator Animator { get; protected set; }
        public string TargetTag { get; protected set; }
        private CharacterSpineController SpineController { get; set; }
        private Player _target;
        public bool IsTriggerd = false;

        private void Awake()
        {
            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = 1.2f;

            TargetTag = Tag.Player;
        }

        public void Set(int characterId, Player target)
        {
            var key = characterId.ToString();
            if (Animator.Target != null)
            {
                if (Animator.Target.name.Contains(key))
                    return;

                Animator.DestroyTarget();
            }

            var go = ResourceManager.Instance.Instantiate(key, gameObject.transform);
            if (go == null)
            {
                NcDebug.LogError($"Missing Spine Resource: {characterId}");
                return;
            }

            SpineController = go.GetComponent<CharacterSpineController>();
            Animator.ResetTarget(go);
            _target = target;
            IsTriggerd = false;
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    AttackEndCalled = true;
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject != _target.gameObject)
            {
                return;
            }
            IsTriggerd = true;
            NcDebug.Log($"[BreakthroughCharacter] OnTriggered {other.name}");

            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            if (Game.instance.Stage.StageSkipCritical)
            {
                ActionCamera.instance.Shake();
            }
            AudioController.PlayDamagedCritical();
            VFXController.instance.Create<AdventureBossSweepAttackVFX>(pos);

            StartCoroutine(Dying());
            transform.DOMove(transform.position + new Vector3(8f, 6f, 0), 1.8f).SetEase(Ease.OutExpo);
            transform.DOBlendablePunchRotation(new Vector3(360, 360, 360), 1.2f).SetEase(Ease.OutExpo);
        }

        protected virtual IEnumerator Dying()
        {
            Animator.Die();
            yield return new WaitForSeconds(.2f);
            //DisableHUD();
            yield return new WaitForSeconds(.8f);
            //OnDeadEnd();
            Animator.DestroyTarget();
        }

    }
}
