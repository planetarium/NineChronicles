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

        private Shader _shader;
        private Spine.Animation _targetAnimation;
        private Sequence _doFadeSequence;
        private GameObject _cachedWeaponVFX;
        private readonly List<Tweener> _fadeTweener = new();
        private bool _isActiveFullCostume;
        private readonly Dictionary<AvatarPartsType, SkeletonAnimation> _parts = new();

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
            // Refresh();
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
            // Refresh();
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

        private void Refresh(bool isReset, bool isDcc)
        {
            if (isDcc)
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
            else
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
                        var value = type is not (AvatarPartsType.ac_eye
                            or AvatarPartsType.ac_face
                            or AvatarPartsType.ac_head
                            or AvatarPartsType.full_costume);

                        if (skeletonAnimation.gameObject.activeSelf != value)
                        {
                            skeletonAnimation.gameObject.SetActive(value);
                        }
                    }
                }

                _cachedWeaponVFX?.SetActive(!_isActiveFullCostume);
            }


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

        public void UpdateFullCostume(int index, bool isDcc)
        {
            _isActiveFullCostume = true;
            UpdateSkeletonDataAsset(index, true, isDcc);
        }

        public void UnequipFullCostume(bool isDcc)
        {
            if (!_isActiveFullCostume)
            {
                return;
            }

            var fullCostume = _parts[AvatarPartsType.full_costume];
            fullCostume.ClearState();
            fullCostume.skeletonDataAsset = null;
            fullCostume.Initialize(true);
            _isActiveFullCostume = false;
            Refresh(true, isDcc);
        }

        public void UpdateBody(int index, int skinTone, bool isDcc)
        {
            if (index == 10255000)
            {
                index = 10235001;
            }
            var s = SplitIndex(index);
            var preIndex = s[0] + s[4] + s[5] + s[6] + s[7];
            var skinName = $"{index}-{skinTone}";
            // Debug.Log($"[UpdateBody] : {preIndex} / {skinName}");
            UpdateSkeletonDataAsset(preIndex, false, isDcc);
            UpdateSkin(true, index, AvatarPartsType.body, skinName);
        }

        private List<int> SplitIndex(int index)
        {
            var result = new List<int>();
            var x = 1;
            while (index != 0)
            {
                var n = index % 10;
                result.Add(n * x);
                index /= 10;
                x *= 10;
            }

            return result;
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

            if (skeletonAnimation.Skeleton.Skin is not null &&
                skeletonAnimation.Skeleton.Skin.Name != string.Empty &&
                skeletonAnimation.Skeleton.Skin.Name == skinName)
            {
                return;
            }

            var skin = skeletonAnimation.Skeleton.Data.FindSkin(skinName);
            if (skin is null)
            {
                return;
            }

            skeletonAnimation.Skeleton.SetSkin(skinName);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            skeletonAnimation.Skeleton.Update(0);
        }

        private void UpdateSkeletonDataAsset(int index, bool isFullCostume, bool isDcc)
        {
            var type = isFullCostume ? AvatarPartsType.full_costume : AvatarPartsType.body;
            var skeletonAnimation = _parts[type];
            var name = isFullCostume ? $"{index}_SkeletonData" : $"body_skin_{index}_SkeletonData";
            if (skeletonAnimation.skeletonDataAsset is not null &&
                skeletonAnimation.skeletonDataAsset.name == name)
            {
                return;
            }

            var asset = isFullCostume
                ? avatarScriptableObject.FullCostume.FirstOrDefault(x => x.name == name)
                : avatarScriptableObject.Body.FirstOrDefault(x => x.name == name);

            skeletonAnimation.ClearState();
            skeletonAnimation.skeletonDataAsset = asset;
            skeletonAnimation.Initialize(true);

            Refresh(true, isDcc);
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
