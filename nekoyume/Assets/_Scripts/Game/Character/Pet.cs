using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Pet : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        [SerializeField] private Transform container;
        [SerializeField] private Vector3 defaultPetPosition;
        [SerializeField] private string followingBoneName;

        public PetAnimator Animator { get; private set; }

        private BoneFollower _boneFollower;
        private Coroutine _playSpecialCoroutine;
        private Coroutine _delayedPlayCoroutine;

        private void Awake()
        {
            _boneFollower = GetComponent<BoneFollower>();
            Animator = new PetAnimator(this);
            Animator.TimeScale = AnimatorTimeScale;
        }

        public void SetPosition(SkeletonAnimation skeletonAnimation, bool isFullCostume)
        {
            _boneFollower.skeletonRenderer = null;
            _boneFollower.enabled = false;

            if (!isFullCostume || skeletonAnimation == null)
            {
                transform.localPosition = defaultPetPosition;
            }
            else
            {
                _boneFollower.skeletonRenderer = skeletonAnimation;
                _boneFollower.Initialize();
                _boneFollower.SetBone(followingBoneName);
                _boneFollower.enabled = true;
            }

            SetSpineObject();
        }

        public void SetSpineObject()
        {
            if (Game.instance.SavedPetId == null)
            {
                if (Animator.Target is not null)
                {
                    Animator.DestroyTarget();
                }

                return;
            }

            var spineResourcePath = $"Character/Pet/{Game.instance.SavedPetId}";
            if (Animator.Target is not null)
            {
                if (Animator.Target.name.Contains(Game.instance.SavedPetId.ToString()))
                {
                    return;
                }

                Animator.DestroyTarget();
            }

            var origin = Resources.Load<GameObject>(spineResourcePath);
            if (!origin)
            {
                throw new FailedToLoadResourceException<GameObject>(spineResourcePath);
            }

            var go = Instantiate(origin, container);

            Animator.ResetTarget(go);

            if (_playSpecialCoroutine != null)
            {
                StopCoroutine(_playSpecialCoroutine);
                _playSpecialCoroutine = null;
            }

            _playSpecialCoroutine = StartCoroutine(CoPlaySpecial());
        }

        private IEnumerator CoPlaySpecial()
        {
            while (Animator.Target is not null)
            {
                yield return new WaitForSeconds(Random.Range(5, 20));

                Animator.Play(PetAnimation.Type.Special);
            }
        }

        public void DelayedPlay(PetAnimation.Type type, float time = 0f)
        {
            if (time <= 0)
            {
                Animator.Play(type);
                return;
            }

            if (_delayedPlayCoroutine != null)
            {
                StopCoroutine(_delayedPlayCoroutine);
                _delayedPlayCoroutine = null;
            }

            _delayedPlayCoroutine = StartCoroutine(CoDelayedPlay(type, time));
        }

        private IEnumerator CoDelayedPlay(PetAnimation.Type type, float time)
        {
            yield return new WaitForSeconds(time);
            Animator.Play(type);
        }

        [ContextMenu("Spawn Pet")]
        public void SpawnPet()
        {
            var petList = new List<int> { 1001, 1002, 1003, 1004 };
            Game.instance.SavedPetId = petList[Random.Range(0, petList.Count)];

            SetSpineObject();
        }
    }
}
