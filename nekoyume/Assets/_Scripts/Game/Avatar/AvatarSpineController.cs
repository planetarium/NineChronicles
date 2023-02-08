using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using UnityEngine;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;

namespace Nekoyume.Game.Avatar
{
    public sealed class AvatarSpineController : MonoBehaviour
    {
        private const string DefaultPmaShader = "Spine/Skeleton";
        private const string WeaponSlot = "weapon";

        [Serializable]
        public class StateNameToAnimationReference
        {
            public string stateName;
            public AnimationReferenceAsset animation;
        }

        [SerializeField]
        private List<AvatarParts> parts;

        [SerializeField]
        private BoxCollider bodyCollider;

        [SerializeField]
        private BoxCollider fullCostumeCollider;

        [SerializeField]
        private AvatarScriptableObject avatarScriptableObject;

        [SerializeField]
        private SkeletonDataAsset testBody;

        private Shader _shader;
        private Material _material;
        private Spine.Animation _targetAnimation;
        private Sequence _doFadeSequence;
        private GameObject _cachedWeaponVFX;
        private readonly List<Tweener> _fadeTweener = new();
        private bool isActiveFullCostume;

        private readonly Dictionary<AvatarPartsType, SkeletonAnimation> _parts = new();

        public BoxCollider Collider => isActiveFullCostume ? fullCostumeCollider : bodyCollider;

        private void Awake()
        {
            foreach (var p in parts)
            {
                _parts.Add(p.Type, p.SkeletonAnimation);
            }

            bodyCollider.enabled = false;
            fullCostumeCollider.enabled = false;
            _shader = Shader.Find(DefaultPmaShader);
            _material = new Material(_shader);
        }

        private void OnEnable()
        {
            UpdateParts();
        }

        private void OnDisable()
        {
            StopFade();
        }

        public SkeletonAnimation GetSkeletonAnimation(AvatarPartsType partsType)
        {
            return _parts.ContainsKey(partsType) ? _parts[partsType] : null;
        }

        public SkeletonAnimation GetSkeletonAnimation()
        {
            return isActiveFullCostume
                ? _parts[AvatarPartsType.full_costume]
                : _parts[AvatarPartsType.body];
        }

        public float GetSpineAlpha()
        {
            var skeletonAnimation = _parts[AvatarPartsType.body];
            if (skeletonAnimation == null)
            {
                return 1;
            }

            return skeletonAnimation.Skeleton.A;
        }

        public void Hide()
        {
            foreach (var skeletonAnimation in _parts.Values)
            {
                skeletonAnimation.gameObject.SetActive(false);
            }
        }

        public void Appear(float duration = 1f, System.Action onComplete = null)
        {
            UpdateActive();
            if (isActiveFullCostume)
            {
                _parts[AvatarPartsType.full_costume].Skeleton.A = 0;
            }
            else
            {
                foreach (var (type, skeletonAnimation) in _parts)
                {
                    if (type == AvatarPartsType.full_costume)
                    {
                        continue;
                    }

                    skeletonAnimation.Skeleton.A = 0;
                }
            }

            StartFade(1f, duration, onComplete);
        }

        public void Disappear(float duration = 1f, System.Action onComplete = null)
        {
            UpdateActive();
            if (isActiveFullCostume)
            {
                _parts[AvatarPartsType.full_costume].Skeleton.A = 1;
            }
            else
            {
                foreach (var (type, skeletonAnimation) in _parts)
                {
                    if (type == AvatarPartsType.full_costume)
                    {
                        continue;
                    }

                    skeletonAnimation.Skeleton.A = 1;
                }
            }

            StartFade(0f, duration, onComplete);
        }

        private void StartFade(float toValue, float duration, System.Action onComplete = null)
        {
            StopFade();
            if (isActiveFullCostume)
            {
                var tweener = DOTween
                    .To(() => _parts[AvatarPartsType.full_costume].skeleton.A,
                        value => _parts[AvatarPartsType.full_costume].skeleton.A = value, toValue,
                        duration)
                    .OnComplete(() => onComplete?.Invoke())
                    .Play();
                _fadeTweener.Add(tweener);
            }
            else
            {
                foreach (var p in _parts.Values)
                {
                    var tweener = DOTween
                        .To(() => p.skeleton.A,
                            value => p.skeleton.A = value, toValue, duration)
                        .OnComplete(() => onComplete?.Invoke())
                        .Play();
                    _fadeTweener.Add(tweener);
                }
            }
        }

        private void StopFade()
        {
            foreach (var tweener in _fadeTweener)
            {
                tweener.Kill();
            }

            _fadeTweener.Clear();
        }

        public void PlayAnimationForState(string stateName, int layerIndex)
        {
            PlayNewAnimation(stateName, layerIndex);
        }

        private void PlayNewAnimation(string animationName, int layerIndex)
        {
            var isLoop = IsLoopAnimation(animationName);
            if (isActiveFullCostume)
            {
                var skeletonAnimation = _parts[AvatarPartsType.full_costume];
                var name = SanitizeAnimationName(skeletonAnimation, animationName);
                var entry = skeletonAnimation.AnimationState.SetAnimation(layerIndex, name, isLoop);
                var duration = skeletonAnimation.Skeleton.Data.FindAnimation(name).Duration;
                entry.TimeScale = duration;
            }
            else
            {
                foreach (var skeletonAnimation in _parts.Values)
                {
                    var name = SanitizeAnimationName(skeletonAnimation, animationName);
                    var entry =
                        skeletonAnimation.AnimationState.SetAnimation(layerIndex, name, isLoop);
                    var duration = skeletonAnimation.Skeleton.Data.FindAnimation(name).Duration;
                    entry.TimeScale = duration;
                    skeletonAnimation.AnimationState.Update(0);
                }
            }
        }

        private string SanitizeAnimationName(SkeletonAnimation skeletonAnimation,
            string animationName)
        {
            var animations = skeletonAnimation.skeleton.Data.Animations;
            if (animations.Exists(x => x.Name == animationName))
            {
                return animationName;
            }

            switch (animationName)
            {
                case "CastingAttack":
                case "CriticalAttack":
                case "Touch":
                    return "Attack";
                default:
                    var splits = animationName.Split('_');
                    var split = splits[0];
                    return animations.Exists(x => x.Name == split) ? split : "Idle";
            }
        }

        private RegionAttachment MakeAttachment(Sprite sprite)
        {
            return sprite.ToRegionAttachmentPMAClone(_shader);
        }

        private bool IsLoopAnimation(string animationName)
        {
            return animationName
                is nameof(CharacterAnimation.Type.Idle)
                or nameof(CharacterAnimation.Type.Run)
                or nameof(CharacterAnimation.Type.Casting)
                or nameof(CharacterAnimation.Type.TurnOver_01);
        }

        // private void UpdateAppearance(bool isFullCostume, AvatarState avatarState, bool isDcc)
        // {
        //     if (isFullCostume)
        //     {
        //
        //     }
        //     else
        //     {
        //         UpdateBody(10252000, 0);
        //
        //         UpdateHairBack(0, isDcc);
        //         UpdateHairFront(0, isDcc);
        //         UpdateEar(0, isDcc);
        //         UpdateTail(0, isDcc);
        //         UpdateFace(0, isDcc); // 기존은 눈 색깔, Dcc는 눈색깔 + 표정
        //
        //         UpdateAcFace(1, isDcc); // 마스크, 주근깨
        //         UpdateAcEye(1, isDcc); // 안경
        //         UpdateAcHead(1, isDcc); // 고양이
        //     }
        // }

        private void UpdateSkin(bool active, AvatarPartsType type, string skinName)
        {
            Debug.Log($"[UpdateSkin] : {type}////{skinName}");
            if (!_parts.ContainsKey(type))
            {
                return;
            }

            var skeletonAnimation = _parts[type];
            skeletonAnimation.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            var skin = skeletonAnimation.Skeleton.Data.FindSkin(skinName);
            if (skin is not null)
            {
                skeletonAnimation.Skeleton.SetSkin(skinName);
            }

            UpdateParts();
        }

        private void UpdateParts()
        {
            foreach (var skeletonAnimation in _parts.Values)
            {
                skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                skeletonAnimation.Update(0);
            }
        }

        public void UpdateHair(int index, bool isDcc)
        {
            UpdateHairBack(index, isDcc);
            UpdateHairFront(index, isDcc);
        }

        private void UpdateHairBack(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.hair_back, skinName);
        }

        private void UpdateHairFront(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.hair_front, skinName);
        }

        public void UpdateTail(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? "" : string.Empty;
            UpdateSkin(isActive, AvatarPartsType.tail, skinName);
        }

        public void UpdateWeapon(int weaponId, GameObject weaponVFXPrefab = null)
        {
            Debug.Log($"[UpdateWeapon] Start");
            if (_parts.ContainsKey(AvatarPartsType.body))
            {
                return;
            }

            var skeletonAnimation = _parts[AvatarPartsType.body];
            var weaponSlotIndex = skeletonAnimation.Skeleton.FindSlotIndex(WeaponSlot);
            var weaponSprite = SpriteHelper.GetPlayerSpineTextureWeapon(weaponId);
            var newWeapon = MakeAttachment(weaponSprite);
            skeletonAnimation.Skeleton.Data.DefaultSkin
                .SetAttachment(weaponSlotIndex, WeaponSlot, newWeapon);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();

            Destroy(_cachedWeaponVFX);

            if (weaponVFXPrefab is null)
            {
                return;
            }

            var parent = new GameObject(weaponId.ToString());
            var boneFollower = parent.AddComponent<BoneFollower>();
            parent.transform.SetParent(transform);
            Instantiate(weaponVFXPrefab, parent.transform);
            var weaponSlot = skeletonAnimation.Skeleton.FindSlot(WeaponSlot);
            var boneName = weaponSlot.Bone.Data.Name;
            boneFollower.SkeletonRenderer = skeletonAnimation;
            boneFollower.SetBone(boneName);
            _cachedWeaponVFX = parent;
            Debug.Log($"[UpdateWeapon] End");
        }

        public void UpdateFullCostume(int index, bool isEquip)
        {
            isActiveFullCostume = isEquip;
            if (isEquip)
            {
                UpdateSkeletonDataAsset(index, true);
            }

            UpdateActive();
        }

        private void UpdateActive()
        {
            if (isActiveFullCostume)
            {
                foreach (var (type, sk) in _parts)
                {
                    sk.gameObject.SetActive(type == AvatarPartsType.full_costume);
                }
            }
            else
            {
                foreach (var (type, sk) in _parts)
                {
                    sk.gameObject.SetActive(type != AvatarPartsType.full_costume);
                }
            }
        }

        public void UpdateBody(int index, int skinTone)
        {
            _parts[AvatarPartsType.body].ClearState();
            // _parts[AvatarPartsType.body].Skeleton.SetSkin("default");
            // _parts[AvatarPartsType.body].Skeleton.SetSlotsToSetupPose();

            UpdateSkeletonDataAsset(index, false);
            var preIndex = (int)(index * 0.0001) * 10000;
            var skinName = $"body_{preIndex}/{index}-{skinTone}";
            // UpdateSkin(true, AvatarPartsType.body, skinName);
            UpdateActive();
        }

        private void UpdateSkeletonDataAsset(int index, bool isFullCostume)
        {
            var name = $"{index}_SkeletonData";
            var asset = isFullCostume
                ? avatarScriptableObject.FullCostume.FirstOrDefault(x => x.name == name)
                : avatarScriptableObject.Body.FirstOrDefault(x => x.name == name);

            if (!isFullCostume)
            {
                asset = testBody;
            }

            var type = isFullCostume ? AvatarPartsType.full_costume : AvatarPartsType.body;
            var skeletonAnimation = _parts[type];
            skeletonAnimation.ClearState();
            skeletonAnimation.skeletonDataAsset = asset;
            skeletonAnimation.Initialize(true);
        }

        public void UpdateFace(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.face, skinName);
        }

        public void UpdateEar(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ear, skinName);
        }

        public void UpdateAcFace(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ac_face, skinName);
        }

        public void UpdateAcEye(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ac_eye, skinName);
        }

        public void UpdateAcHead(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ac_head, skinName);
        }
    }
}
