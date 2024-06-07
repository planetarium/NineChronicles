using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nekoyume.Game.Character;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.TestScene;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Planetarium.Nekoyume.Editor
{
    // TODO: Costume, NPC 로직 추가
    // TODO: 사용자가 알기 쉽게 예외 상황 전부 알림 띄워주기.
    public static class SpineEditor
    {
        private const string CharacterAnimatorFindAssetFilter = "CharacterAnimator t:AnimatorController";
        private const string PetAnimatorFindAssetFilter = "PetAnimator t:AnimatorController";

        private const string FullCostumePrefabPath = "Assets/Resources/Character/FullCostume";

        private const string FullCostumeSpineRootPath =
            "Assets/AddressableAssets/Character/FullCostume";

        private const string MonsterPrefabPath = "Assets/Resources/Character/Monster";
        private const string MonsterSpineRootPath = "Assets/AddressableAssets/Character/Monster";

        private const string NPCPrefabPath = "Assets/Resources/Character/NPC";
        private const string NPCSpineRootPath = "Assets/AddressableAssets/Character/NPC";

        private const string PlayerPrefabPath = "Assets/Resources/Character/Player";
        private const string PlayerSpineRootPath = "Assets/AddressableAssets/Character/Player";

        private const string PetPrefabPath = "Assets/Resources/Character/Pet";
        private const string PetSpineRootPath = "Assets/AddressableAssets/Character/Pet";

        private static readonly Vector3 Position = Vector3.zero;

        /// <summary>
        /// 헤어 스타일을 결정하는 정보를 스파인이 포함하지 않기 때문에 이곳에 하드코딩해서 구분해 준다.
        /// </summary>
        private static readonly string[] HairType1Names =
        {
            "10230000", "10231000", "10232000", "10233000", "10234000", "10235000"
        };

        [MenuItem("Assets/9C/Create Spine Prefab", true)]
        public static bool CreateSpinePrefabValidation()
        {
            return Selection.activeObject is SkeletonDataAsset;
        }

        [MenuItem("Assets/9C/Create Spine Prefab", false, 0)]
        public static void CreateSpinePrefab()
        {
            if (!(Selection.activeObject is SkeletonDataAsset skeletonDataAsset))
            {
                return;
            }

            CreateSpinePrefab(skeletonDataAsset);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All FullCostume)", false, 0)]
        public static void CreateSpinePrefabAllOfFullCostume()
        {
            CreateSpinePrefabAllOfPath(FullCostumeSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Monster)", false, 0)]
        public static void CreateSpinePrefabAllOfMonster()
        {
            CreateSpinePrefabAllOfPath(MonsterSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All NPC)", false, 0)]
        public static void CreateSpinePrefabAllOfNPC()
        {
            CreateSpinePrefabAllOfPath(NPCSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Player)", false, 0)]
        public static void CreateSpinePrefabAllOfPlayer()
        {
            CreateSpinePrefabAllOfPath(PlayerSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Pet)", false, 0)]
        public static void CreateSpinePrefabAllOfPet()
        {
            CreateSpinePrefabAllOfPath(PetSpineRootPath);
        }

        private static string GetPrefabPath(string prefabName)
        {
            string pathFormat = null;
            if (SpineCharacterViewer.IsFullCostume(prefabName))
            {
                pathFormat = FullCostumePrefabPath;
            }

            if (SpineCharacterViewer.IsMonster(prefabName))
            {
                pathFormat = MonsterPrefabPath;
            }

            if (SpineCharacterViewer.IsNPC(prefabName))
            {
                pathFormat = NPCPrefabPath;
            }

            if (SpineCharacterViewer.IsPlayer(prefabName))
            {
                pathFormat = PlayerPrefabPath;
            }

            if (SpineCharacterViewer.IsPet(prefabName))
            {
                pathFormat = PetPrefabPath;
            }

            return string.IsNullOrEmpty(pathFormat)
                ? null
                : Path.Combine(pathFormat, $"{prefabName}.prefab");
        }

        private static void CreateSpinePrefabInternal(SkeletonDataAsset skeletonDataAsset)
        {
            var assetPath = AssetDatabase.GetAssetPath(skeletonDataAsset);
            var assetFolderPath = assetPath.Replace(Path.GetFileName(assetPath), "");
            var animationAssetsPath = Path.Combine(assetFolderPath, "ReferenceAssets");
            var split = assetPath.Split('/');
            var prefabName = split[split.Length > 1 ? split.Length - 2 : 0];
            var prefabPath = GetPrefabPath(prefabName);

            if (!ValidateSpineResource(prefabName, skeletonDataAsset))
            {
                if (SpineCharacterViewer.IsPlayer(prefabName))
                {
                    Debug.LogError("ValidationSpineResource() return false");
                    return;
                }

                Debug.LogWarning("ValidationSpineResource() return false");
            }

            CreateAnimationReferenceAssets(skeletonDataAsset);

            var skeletonAnimation = EditorInstantiation.InstantiateSkeletonAnimation(
                    skeletonDataAsset);
            skeletonAnimation.AnimationName = nameof(CharacterAnimation.Type.Idle);

            var gameObject = skeletonAnimation.gameObject;
            gameObject.name = prefabName;
            gameObject.layer = LayerMask.NameToLayer("Character");
            gameObject.transform.position = Position;
            gameObject.transform.localScale = GetPrefabLocalScale(prefabName);

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingLayerName = "Character";

            string findAssetFilter;
            if (SpineCharacterViewer.IsPet(prefabName))
            {
                meshRenderer.sortingOrder = 1;
                findAssetFilter = PetAnimatorFindAssetFilter;
            }
            else
            {
                findAssetFilter = CharacterAnimatorFindAssetFilter;
            }

            var animatorControllerGuidArray = AssetDatabase.FindAssets(findAssetFilter);
            if (animatorControllerGuidArray.Length == 0)
            {
                Object.DestroyImmediate(gameObject);
                throw new AssetNotFoundException(
                    $"AssetDatabase.FindAssets(\"{findAssetFilter}\")");
            }

            var animatorControllerPath =
                AssetDatabase.GUIDToAssetPath(animatorControllerGuidArray[0]);
            var animator = gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorControllerPath);

            var controller = AddStatesAndAnimations(prefabName, animationAssetsPath, gameObject);
            if (controller is PlayerSpineController playerSpineController)
            {
                playerSpineController.hairTypeIndex = HairType1Names.Contains(prefabName)
                    ? 1
                    : 0;
            }

            if (File.Exists(prefabPath))
            {
                var boxCollider = controller.GetComponent<BoxCollider>();
                var sourceBoxCollider = AssetDatabase.LoadAssetAtPath<BoxCollider>(prefabPath);
                boxCollider.center = sourceBoxCollider.center;
                boxCollider.size = sourceBoxCollider.size;

                AssetDatabase.DeleteAsset(prefabPath);
            }

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
                Object.DestroyImmediate(gameObject);
                Selection.activeObject = prefab;
            }
            catch
            {
                Object.DestroyImmediate(gameObject);
                throw new FailedToSaveAsPrefabAssetException(prefabPath);
            }
        }

        private static SpineController AddStatesAndAnimations(
            string prefabName,
            string animationAssetsPath,
            GameObject gameObject)
        {
            var controller = GetOrCreateSpineController(prefabName, gameObject);
            switch (controller)
            {
                case PlayerSpineController playerSpineController:
                case CharacterSpineController characterSpineController:
                    foreach (var animationType in CharacterAnimation.List)
                    {
                        var assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                        var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                        if (asset is null)
                        {
                            switch (animationType)
                            {
                                // todo: `CharacterAnimation.Type.Appear`와 `CharacterAnimation.Type.Disappear`는 없어질 예정.
                                default:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(
                                        assetPath);
                                    break;
                                case CharacterAnimation.Type.Idle:
                                    Object.DestroyImmediate(gameObject);
                                    throw new AssetNotFoundException(assetPath);
                                case CharacterAnimation.Type.Win_02:
                                case CharacterAnimation.Type.Win_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(CharacterAnimation.Type.Win)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(
                                        assetPath);
                                    break;
                                case CharacterAnimation.Type.Touch:
                                case CharacterAnimation.Type.CastingAttack:
                                case CharacterAnimation.Type.CriticalAttack:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(CharacterAnimation.Type.Attack)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(
                                        assetPath);
                                    break;
                                case CharacterAnimation.Type.TurnOver_01:
                                case CharacterAnimation.Type.TurnOver_02:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(CharacterAnimation.Type.Die)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(
                                        assetPath);
                                    break;
                            }

                            if (asset is null)
                            {
                                assetPath = Path.Combine(
                                    animationAssetsPath,
                                    $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                                asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            }

                            if (asset is null)
                            {
                                Object.DestroyImmediate(gameObject);
                                throw new AssetNotFoundException(assetPath);
                            }
                        }

                        controller.statesAndAnimations.Add(
                            new SpineController.StateNameToAnimationReference
                            {
                                stateName = animationType.ToString(),
                                animation = asset
                            });
                    }
                    break;
                case NPCSpineController npcSpineController:
                    foreach (var animationType in NPCAnimation.List)
                    {
                        var assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                        var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                        if (asset is null)
                        {
                            switch (animationType)
                            {
                                case NPCAnimation.Type.Appear_02:
                                case NPCAnimation.Type.Appear_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Appear)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Greeting_02:
                                case NPCAnimation.Type.Greeting_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Greeting)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Open_02:
                                case NPCAnimation.Type.Open_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Open)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Idle_02:
                                case NPCAnimation.Type.Idle_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Idle)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Emotion_02:
                                case NPCAnimation.Type.Emotion_03:
                                case NPCAnimation.Type.Emotion_04:
                                case NPCAnimation.Type.Emotion_05:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Emotion)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Touch_02:
                                case NPCAnimation.Type.Touch_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Touch)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Loop_02:
                                case NPCAnimation.Type.Loop_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Loop)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                                case NPCAnimation.Type.Disappear_02:
                                case NPCAnimation.Type.Disappear_03:
                                    assetPath = Path.Combine(
                                        animationAssetsPath,
                                        $"{nameof(NPCAnimation.Type.Disappear)}.asset");
                                    asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                                    break;
                            }

                            if (asset is null)
                            {
                                assetPath = Path.Combine(
                                    animationAssetsPath,
                                    $"{nameof(NPCAnimation.Type.Idle)}.asset");
                                asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            }

                            if (asset is null)
                            {
                                Object.DestroyImmediate(gameObject);
                                throw new AssetNotFoundException(assetPath);
                            }
                        }

                        controller.statesAndAnimations.Add(
                            new SpineController.StateNameToAnimationReference
                            {
                                stateName = animationType.ToString(),
                                animation = asset
                            });
                    }
                    break;
                case PetSpineController petSpineController:
                    foreach (var animationType in PetAnimation.List)
                    {
                        var assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                        var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                        if (asset is null)
                        {
                            Object.DestroyImmediate(gameObject);
                            throw new AssetNotFoundException(assetPath);
                        }

                        controller.statesAndAnimations.Add(
                            new SpineController.StateNameToAnimationReference
                            {
                                stateName = animationType.ToString(),
                                animation = asset
                            });
                    }

                    break;
            }

            return controller;
        }

        #region Validate Spine Resource

        private static bool ValidateSpineResource(
            string prefabName,
            SkeletonDataAsset skeletonDataAsset)
        {
            if (SpineCharacterViewer.IsFullCostume(prefabName))
            {
                return ValidateForFullCostume(skeletonDataAsset);
            }

            if (SpineCharacterViewer.IsMonster(prefabName))
            {
                return ValidateForMonster(skeletonDataAsset);
            }

            if (SpineCharacterViewer.IsNPC(prefabName))
            {
                return ValidateForNPC(skeletonDataAsset);
            }

            if (SpineCharacterViewer.IsPlayer(prefabName))
            {
                return ValidateForPlayer(skeletonDataAsset);
            }

            if (SpineCharacterViewer.IsPet(prefabName))
            {
                return ValidateForPet(skeletonDataAsset);
            }

            return false;
        }

        private static bool ValidateForFullCostume(SkeletonDataAsset skeletonDataAsset) =>
            ValidateForPlayer(skeletonDataAsset);

        private static bool ValidateForMonster(SkeletonDataAsset skeletonDataAsset)
        {
            var data = skeletonDataAsset.GetSkeletonData(false);
            var hud = data.FindBone("HUD");

            return !(hud is null);
        }

        private static bool ValidateForNPC(SkeletonDataAsset skeletonDataAsset) => true;

        private static bool ValidateForPlayer(SkeletonDataAsset skeletonDataAsset)
        {
            var result = true;
            var data = skeletonDataAsset.GetSkeletonData(false);
            var hud = data.FindBone("HUD");
            if (hud is null)
            {
                Debug.LogError("NotFoundBone: HUD");
                result = false;
            }

            var slotNames = new[]
            {
                PlayerSpineController.WeaponSlot,
                PlayerSpineController.EarLeftSlot,
                PlayerSpineController.EarRightSlot,
                PlayerSpineController.EyeHalfSlot,
                PlayerSpineController.EyeOpenSlot,
            };
            foreach (var slotName in slotNames)
            {
                var weaponSlot = data.FindSlot(slotName);
                if (weaponSlot is null)
                {
                    Debug.LogError($"NotFoundSlot: {slotName}");
                    result = false;
                }
            }

            return result;
        }

        private static bool ValidateForPet(SkeletonDataAsset skeletonDataAsset) => true;

        #endregion

        // CharacterAnimation.Type에서 포함하지 않는 것을 이곳에서 걸러낼 수도 있겠다.
        /// <summary>
        /// `SkeletonDataAssetInspector.CreateAnimationReferenceAssets(): 242`
        /// </summary>
        /// <param name="skeletonDataAsset"></param>
        private static void CreateAnimationReferenceAssets(SkeletonDataAsset skeletonDataAsset)
        {
            const string assetFolderName = "ReferenceAssets";

            var parentFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(skeletonDataAsset));
            var dataPath = parentFolder + "/" + assetFolderName;
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                AssetDatabase.CreateFolder(parentFolder, assetFolderName);
            }

            var nameField =
                typeof(AnimationReferenceAsset).GetField(
                    "animationName",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            if (nameField is null)
            {
                throw new NullReferenceException(
                    "typeof(AnimationReferenceAsset).GetField(\"animationName\", BindingFlags.NonPublic | BindingFlags.Instance);");
            }

            var skeletonDataAssetField = typeof(AnimationReferenceAsset).GetField(
                "skeletonDataAsset",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (skeletonDataAssetField is null)
            {
                throw new NullReferenceException(
                    "typeof(AnimationReferenceAsset).GetField(\"skeletonDataAsset\", BindingFlags.NonPublic | BindingFlags.Instance);");
            }

            var skeletonData = skeletonDataAsset.GetSkeletonData(false);
            foreach (var animation in skeletonData.Animations)
            {
                var assetPath =
                    $"{dataPath}/{AssetUtility.GetPathSafeName(animation.Name)}.asset";

                var newAsset = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
                skeletonDataAssetField.SetValue(newAsset, skeletonDataAsset);
                nameField.SetValue(newAsset, animation.Name);
                AssetDatabase.CreateAsset(newAsset, assetPath);
            }

            var folderObject = AssetDatabase.LoadAssetAtPath(dataPath, typeof(Object));
            if (!(folderObject is null))
            {
                Selection.activeObject = folderObject;
                EditorGUIUtility.PingObject(folderObject);
            }
        }

        private static SpineController GetOrCreateSpineController(string prefabName,
            GameObject target)
        {
            if (SpineCharacterViewer.IsPlayer(prefabName) ||
                SpineCharacterViewer.IsFullCostume(prefabName))
            {
                return target.AddComponent<PlayerSpineController>();
            }

            if (SpineCharacterViewer.IsNPC(prefabName))
            {
                return target.AddComponent<NPCSpineController>();
            }

            if (SpineCharacterViewer.IsPet(prefabName))
            {
                return target.AddComponent<PetSpineController>();
            }

            return target.AddComponent<CharacterSpineController>();
        }

        private static void CreateSpinePrefabAllOfPath(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Debug.LogWarning($"Not Found Folder! {path}");
                return;
            }

            var subFolderPaths = AssetDatabase.GetSubFolders(path);
            foreach (var subFolderPath in subFolderPaths)
            {
                var id = Path.GetFileName(subFolderPath);
                var skeletonDataAssetPath = Path.Combine(subFolderPath, $"{id}_SkeletonData.asset");
                Debug.Log($"Try to create spine prefab with {skeletonDataAssetPath}");
                var skeletonDataAsset =
                    AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataAssetPath);
                if (ReferenceEquals(skeletonDataAsset, null) || skeletonDataAsset == null)
                {
                    Debug.LogError($"Not Found SkeletonData from {skeletonDataAssetPath}");
                    continue;
                }

                try
                {
                    CreateSpinePrefab(skeletonDataAsset);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }
            }
        }

        private static void CreateSpinePrefab(SkeletonDataAsset skeletonDataAsset)
        {
            try
            {
                CreateSpinePrefabInternal(skeletonDataAsset);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Unexpected exception thrown while creating spine prefab.\n{e}",
                    "OK");
                throw e;
            }
        }

        // NOTE: 모든 캐릭터는 원본의 해상도를 보여주기 위해서 Vector3.one 사이즈로 스케일되어야 맞습니다.
        // 하지만 이 프로젝트는 2D 리소스의 ppu와 카메라 사이즈가 호환되지 않아서 임의의 스케일을 설정합니다.
        // 이마저도 아트 단에서 예상하지 못한 스케일 이슈가 생기면 "300005"와 같이 예외적인 케이스가 발생합니다.
        // 앞으로 이런 예외가 많아질 것을 대비해서 별도의 함수로 뺍니다.
        private static Vector3 GetPrefabLocalScale(string prefabName)
        {
            switch (prefabName)
            {
                default:
                    return new Vector3(.64f, .64f, 1f);
                case "300005":
                    return new Vector3(.8f, .8f, 1f);
            }
        }
    }
}
