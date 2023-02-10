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
        private Spine.Animation _targetAnimation;
        private Sequence _doFadeSequence;
        private GameObject _cachedWeaponVFX;
        private readonly List<Tweener> _fadeTweener = new();
        private bool _isActiveFullCostume;
        private readonly Dictionary<AvatarPartsType, SkeletonAnimation> _parts = new();
        private readonly Dictionary<AvatarPartsType, int> _partsIndex = new()
        {
            { AvatarPartsType.hair_back, -1 },
            { AvatarPartsType.tail, -1 },
            { AvatarPartsType.body, -1 },
            { AvatarPartsType.face, -1 },
            { AvatarPartsType.ear, -1 },
            { AvatarPartsType.ac_face, -1 },
            { AvatarPartsType.ac_eye, -1 },
            { AvatarPartsType.hair_front, -1 },
            { AvatarPartsType.ac_head, -1 },
            { AvatarPartsType.full_costume, -1 },
        };

        public BoxCollider Collider => _isActiveFullCostume ? fullCostumeCollider : bodyCollider;

        private void Awake()
        {
            foreach (var p in parts)
            {
                _parts.Add(p.Type, p.SkeletonAnimation);
            }

            bodyCollider.enabled = false;
            fullCostumeCollider.enabled = false;
            _shader = Shader.Find(DefaultPmaShader);
        }

        private void OnDisable()
        {
            StopFade();
        }

        public SkeletonAnimation GetSkeletonAnimation()
        {
            return _isActiveFullCostume
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
            Refresh();
            if (_isActiveFullCostume)
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
            Refresh();
            if (_isActiveFullCostume)
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

        public void Refresh(bool isReset = false)
        {
            if (_isActiveFullCostume)
            {
                foreach (var (type, skeletonAnimation) in _parts)
                {
                    var value = type == AvatarPartsType.full_costume;
                    if (skeletonAnimation.gameObject.activeSelf != value)
                    {
                        skeletonAnimation.gameObject.SetActive(value);
                    }
                }
            }
            else
            {
                foreach (var (type, skeletonAnimation) in _parts)
                {
                    var value = type != AvatarPartsType.full_costume;
                    if (skeletonAnimation.gameObject.activeSelf != value)
                    {
                        skeletonAnimation.gameObject.SetActive(value);
                    }
                }
            }

            Debug.Log($"[Refresh] :{isReset}");
            if (isReset)
            {
                foreach (var sa in _parts.Values.Where(x => x.isActiveAndEnabled))
                {
                    sa.Skeleton.SetSlotsToSetupPose();
                }

                PlayAnimation("Idle", 0);
            }
        }

        public void PlayAnimation(string animationName, int layerIndex)
        {
            var isLoop = IsLoopAnimation(animationName);
            foreach (var (type, skeletonAnimation) in _parts)
            {
                if (!skeletonAnimation.isActiveAndEnabled)
                {
                    continue;
                }

                var name = SanitizeAnimationName(skeletonAnimation, animationName);
                var entry =
                    skeletonAnimation.AnimationState.SetAnimation(layerIndex, name, isLoop);
                var duration = skeletonAnimation.Skeleton.Data.FindAnimation(name).Duration;
                entry.TimeScale = duration;
                skeletonAnimation.AnimationState.Update(0);
            }
        }

        public void UpdateWeapon(int weaponId, GameObject weaponVFXPrefab = null)
        {
            if (!_parts.ContainsKey(AvatarPartsType.body))
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
        }

        public void UpdateFullCostume(int index)
        {
            _isActiveFullCostume = true;
            UpdateSkeletonDataAsset(index, true);
        }

        public void UnequipFullCostume()
        {
            _isActiveFullCostume = false;
            Refresh(true);
        }

        public void UpdateBody(int index, int skinTone)
        {
            UpdateSkeletonDataAsset(index, false);
            var preIndex = (int)(index * 0.0001) * 10000;
            var skinName = $"body_{preIndex}/{index}-{skinTone}";
            // UpdateSkin(true, AvatarPartsType.body, skinName);
        }

        public void UpdateHair(int index, bool isDcc)
        {
            UpdateHairBack(index, isDcc);
            UpdateHairFront(index, isDcc);
        }

        private void UpdateHairBack(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.hair_back, skinName);
        }

        private void UpdateHairFront(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.hair_front, skinName);
        }

        public void UpdateTail(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.tail, skinName);
        }

        public void UpdateFace(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.face, skinName);
        }

        public void UpdateEar(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.ear, skinName);
        }

        public void UpdateAcFace(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.ac_face, skinName);
        }

        public void UpdateAcEye(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.ac_eye, skinName);
        }

        public void UpdateAcHead(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var skinName = isDcc ? $"DCC_{index}" : $"{index}";
            UpdateSkin(isActive, index, AvatarPartsType.ac_head, skinName);
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

        private void UpdateSkin(bool active, int index, AvatarPartsType type, string skinName)
        {
            if (_partsIndex[type] == index)
            {
                return;
            }

            _partsIndex[type] = index;

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
            if (skin is null)
            {
                return;
            }

            Debug.Log($"[UpdateSkin] {type} : {skinName}");
            var p = parts.FirstOrDefault(x => x.Type == type);
            p.SkeletonAnimation.Skeleton.SetSkin(skinName);
            p.SkeletonAnimation.Skeleton.SetSlotsToSetupPose();
            p.SkeletonAnimation.Skeleton.Update(0);
        }

        private void UpdateSkeletonDataAsset(int index, bool isFullCostume)
        {
            var type = isFullCostume ? AvatarPartsType.full_costume : AvatarPartsType.body;
            if (_partsIndex[type] == index)
            {
                return;
            }

            _partsIndex[type] = index;

            var name = $"{index}_SkeletonData";
            var asset = isFullCostume
                ? avatarScriptableObject.FullCostume.FirstOrDefault(x => x.name == name)
                : avatarScriptableObject.Body.FirstOrDefault(x => x.name == name);

            if (!isFullCostume)
            {
                Debug.Log("TEST BODY");
                asset = testBody;
            }

            var skeletonAnimation = _parts[type];
            skeletonAnimation.ClearState();
            skeletonAnimation.skeletonDataAsset = asset;
            skeletonAnimation.Initialize(true);
            Refresh(true);
        }

        private void StartFade(float toValue, float duration, System.Action onComplete = null)
        {
            StopFade();
            if (_isActiveFullCostume)
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
                foreach (var (type, skeletonAnimation) in _parts)
                {
                    if (type == AvatarPartsType.full_costume)
                    {
                        continue;
                    }

                    var tweener = DOTween
                        .To(() => skeletonAnimation.skeleton.A,
                            value => skeletonAnimation.skeleton.A = value, toValue, duration)
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
    }
}
