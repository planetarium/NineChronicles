using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using UnityEngine;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;

namespace Nekoyume.Game.Avatar
{
    public sealed class AvatarSpineController : MonoBehaviour
    {
        private const string DefaultPmaShader = "Spine/Skeleton Tint";
        private const string WeaponSlot = "weapon";

        [SerializeField]
        private Character.Character owner;

        [SerializeField]
        private List<AvatarParts> parts;

        [SerializeField]
        private BoxCollider bodyCollider;

        [SerializeField]
        private BoxCollider fullCostumeCollider;

        [SerializeField]
        private GameObject auraPos;

        private Shader _shader;
        private Spine.Animation _targetAnimation;
        private DG.Tweening.Sequence _doFadeSequence;
        private GameObject _cachedWeaponVFX;
        private GameObject _cachedAuraVFX;
        private readonly List<Tweener> _fadeTweener = new();
        private bool _isActiveFullCostume;
        private readonly Dictionary<AvatarPartsType, SkeletonAnimation> _parts = new();
        private GameObject _prevAuraPrefab;
        private GameObject _prevWeaponObj;

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

        public SkeletonAnimation GetBodySkeletonAnimation()
        {
            return _isActiveFullCostume
                ? _parts[AvatarPartsType.full_costume]
                : _parts[AvatarPartsType.body_back];
        }

        public SkeletonAnimation GetSkeletonAnimation(AvatarPartsType partsType)
        {
            return _parts[partsType];
        }

        public float GetSpineAlpha()
        {
            var skeletonAnimation = _parts[AvatarPartsType.body_back];
            if (skeletonAnimation is null)
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

        private void Refresh(bool isDcc)
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
                    Destroy(_cachedWeaponVFX);
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
            }

            foreach (var sa in _parts.Values.Where(x => x.isActiveAndEnabled))
            {
                sa.Skeleton.SetSlotsToSetupPose();
            }

            PlayAnimation("Idle", 0);
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
            if (!_parts.ContainsKey(AvatarPartsType.body_front))
            {
                return;
            }

            var skeletonAnimation = _parts[AvatarPartsType.body_front];
            var weaponSlot        = skeletonAnimation.Skeleton.FindSlot(WeaponSlot);
            var weaponSlotIndex   = weaponSlot == null ? -1 : weaponSlot.Data.Index;
            var weaponSprite      = SpriteHelper.GetPlayerSpineTextureWeapon(weaponId);
            var newWeapon         = MakeAttachment(weaponSprite);
            skeletonAnimation.Skeleton.Data.DefaultSkin
                .SetAttachment(weaponSlotIndex, WeaponSlot, newWeapon);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            SetVisibleBodyParts(AvatarPartsType.body_back, true);
            SetVisibleBodyParts(AvatarPartsType.body_front, false);

            if(_prevWeaponObj == weaponVFXPrefab)
            {
                return;
            }
            _prevWeaponObj = weaponVFXPrefab;

            Destroy(_cachedWeaponVFX);

            if (weaponVFXPrefab is null)
            {
                return;
            }

            var parent = new GameObject(weaponId.ToString());
            var boneFollower = parent.AddComponent<BoneFollower>();
            parent.transform.SetParent(transform);
            Instantiate(weaponVFXPrefab, parent.transform);
            var boneName = weaponSlot?.Bone.Data.Name ?? string.Empty;
            boneFollower.SkeletonRenderer = skeletonAnimation;
            boneFollower.SetBone(boneName);
            _cachedWeaponVFX = parent;
        }

        public void UpdateAura(GameObject auraVFXPrefab = null)
        {
            if (_prevAuraPrefab == auraVFXPrefab)
            {
                if (auraVFXPrefab == null || _cachedAuraVFX == null)
                {
                    return;
                }
                
                if (_cachedAuraVFX.TryGetComponent(out AuraPrefabBase cachedAuraObject))
                {
                    cachedAuraObject.Owner = owner;
                }
                return;
            }
            _prevAuraPrefab = auraVFXPrefab;

            Destroy(_cachedAuraVFX);

            if(auraVFXPrefab == null)
            {
                auraPos.SetActive(false);
                return;
            }

            auraPos.SetActive(true);
            var vfx = Instantiate(auraVFXPrefab, auraPos.transform);
            vfx.transform.localPosition = Vector3.zero;
            if (vfx.TryGetComponent(out AuraPrefabBase auraPrefabBase))
            {
                auraPrefabBase.Owner = owner;
            }

            _cachedAuraVFX = vfx;
        }

        public void UpdateFullCostume(int index, bool isDcc)
        {
            _isActiveFullCostume = true;
            var key   = $"{index}_SkeletonData";
            var asset = ResourceManager.Instance.Load<SkeletonDataAsset>(key);

            if (asset == null)
            {
                NcDebug.LogError($"Failed to load SkeletonDataAsset: {key}");
                return;
            }

            var isChange = UpdateSkeletonDataAsset(AvatarPartsType.full_costume, asset);
            if (isChange)
            {
                Refresh(isDcc);
            }
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
            Refresh(isDcc);
        }

        public void UpdateBody(int index, int skinTone, bool isDcc)
        {
            if (index == 10255000)
            {
                index = 10235001;
            }
            var s = SplitIndex(index);
            var preIndex = s[0] + s[4] + s[5] + s[6] + s[7];
            if (skinTone > 0)
            {
                skinTone -= 1;
            }
            var skinName = $"{index}-{skinTone}";

            var key = $"body_skin_{preIndex}_SkeletonData";
            var asset = ResourceManager.Instance.Load<SkeletonDataAsset>(key);

            if (asset == null)
            {
                NcDebug.LogError($"Failed to load SkeletonDataAsset: {key}");
                return;
            }

            var isUpdatedAsset = UpdateSkeletonDataAsset(AvatarPartsType.body_back, asset);
            UpdateSkeletonDataAsset(AvatarPartsType.body_front, asset);
            var isUpdatedSkin = UpdateSkin(true, AvatarPartsType.body_back, skinName);
            UpdateSkin(true, AvatarPartsType.body_front, skinName);
            if (isUpdatedAsset || isUpdatedSkin)
            {
                Refresh(isDcc);
                SetVisibleBodyParts(AvatarPartsType.body_back, true);
                SetVisibleBodyParts(AvatarPartsType.body_front, false);
            }
        }

        private void SetVisibleBodyParts(AvatarPartsType type, bool removeWeapon)
        {
            var skeletonAnimation = _parts[type];
            var list = new List<string>
            {
                "hand_L", "arm_L", "shoulder_L", "weapon"
            };

            foreach (var slot in skeletonAnimation.Skeleton.Slots)
            {
                if (slot.Attachment is null)
                {
                    continue;
                }

                if (removeWeapon)
                {
                    if (list.Any(x=> slot.ToString().Contains(x)))
                    {
                        slot.Attachment = null;
                    }
                }
                else
                {
                    if (!list.Any(x=> slot.ToString().Contains(x)))
                    {
                        slot.Attachment = null;
                    }
                }

            }
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
            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            if (index == 1 && isDcc)
            {
                skinName = "40200001";
            }
            UpdateSkin(isActive, AvatarPartsType.hair_back, skinName);
        }

        private void UpdateHairFront(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            if (index == 1 && isDcc)
            {
                skinName = "40200001";
            }
            UpdateSkin(isActive, AvatarPartsType.hair_front, skinName);
        }

        public void UpdateTail(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            if (index == 1 && isDcc)
            {
                skinName = "40500001";
            }
            UpdateSkin(isActive, AvatarPartsType.tail, skinName);
        }

        public void UpdateFace(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.face, skinName);
        }

        public void UpdateEar(int index, bool isDcc)
        {
            var isActive = !(isDcc && index == 0);
            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            if (index == 1 && isDcc)
            {
                skinName = "40300001";
            }
            UpdateSkin(isActive, AvatarPartsType.ear, skinName);
        }

        public void UpdateAcFace(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ac_face, skinName);
        }

        public void UpdateAcEye(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";;
            UpdateSkin(isActive, AvatarPartsType.ac_eye, skinName);
        }

        public void UpdateAcHead(int index, bool isDcc)
        {
            var isActive = isDcc;
            if (isDcc && index == 0)
            {
                isActive = false;
            }

            var zero = index < 10 ? "0" : string.Empty;
            var skinName = isDcc ? $"DCC_{zero}{index}" : $"{index}";
            UpdateSkin(isActive, AvatarPartsType.ac_head, skinName);
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

        private bool UpdateSkin(bool active, AvatarPartsType type, string skinName)
        {
            if (!_parts.ContainsKey(type))
            {
                return false;
            }

            var skeletonAnimation = _parts[type];
            skeletonAnimation.gameObject.SetActive(active);
            if (!active)
            {
                return false;
            }

            if (skeletonAnimation.Skeleton.Skin is not null &&
                skeletonAnimation.Skeleton.Skin.Name != string.Empty &&
                skeletonAnimation.Skeleton.Skin.Name == skinName)
            {
                return false;
            }

            var skin = skeletonAnimation.Skeleton.Data.FindSkin(skinName);
            if (skin is null)
            {
                return false;
            }

            skeletonAnimation.Skeleton.SetSkin(skinName);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            return true;
        }

        private bool UpdateSkeletonDataAsset(AvatarPartsType type, SkeletonDataAsset asset)
        {
            var skeletonAnimation = _parts[type];
            if (skeletonAnimation.skeleton is not null &&
                skeletonAnimation.skeletonDataAsset.name == asset.name)
            {
                return false;
            }

            skeletonAnimation.ClearState();
            skeletonAnimation.skeletonDataAsset = asset;
            skeletonAnimation.Initialize(true);
            return true;
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

        public void SetSpineColor(Color color, int propertyID)
        {
            foreach (var skeletonAnimation in _parts.Values)
            {
                if (skeletonAnimation == null)
                {
                    continue;
                }

                if (skeletonAnimation.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    var mpb = new MaterialPropertyBlock();
                    meshRenderer.GetPropertyBlock(mpb);
                    mpb.SetColor(propertyID, color);
                    meshRenderer.SetPropertyBlock(mpb);
                }
                else
                {
                    NcDebug.LogError($"[{nameof(AvatarSpineController)}] No MeshRenderer found in {skeletonAnimation.name}.");
                }
            }
        }
    }
}
