using System;
using System.IO;
using System.Reflection;
using Nekoyume.Game.Character;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Planetarium.Nekoyume.Editor
{
    // todo: NPC 로직 추가
    public static class SpineEditor
    {
        private const string FindAssetFilter = "CharacterAnimator t:AnimatorController";
        private const string PlayerPrefabPath = "Assets/Resources/Character/Player";
        private const string MonsterPrefabPath = "Assets/Resources/Character/Monster";
        private const string NPCPrefabPath = "Assets/Resources/Character/NPC";
        private const string PlayerSpineRootPath = "Assets/AddressableAssets/Character/Player";
        private const string MonsterSpineRootPath = "Assets/AddressableAssets/Character/Monster";
        private const string NPCSpineRootPath = "Assets/AddressableAssets/Character/NPC";

        private static readonly Vector3 Position = Vector3.zero;
        private static readonly Vector3 LocalScale = new Vector3(.64f, .64f, 1f);

        [MenuItem("Assets/9C/Create Spine Prefab", true)]
        public static bool CreateSpinePrefabValidation()
        {
            return Selection.activeObject is SkeletonDataAsset;
        }

        [MenuItem("Assets/9C/Create Spine Prefab", false, 0)]
        public static void CreateSpinePrefab()
        {
            if (!(Selection.activeObject is SkeletonDataAsset skeletonDataAsset))
                return;

            CreateSpinePrefabInternal(skeletonDataAsset);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Player)", false, 0)]
        public static void CreateSpinePrefabAllOfPlayer()
        {
            CreateSpinePrefabAllOfPath(PlayerSpineRootPath);
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

        private static void CreateSpinePrefabInternal(SkeletonDataAsset skeletonDataAsset)
        {
            if (!ValidateForPlayerOrMonster(skeletonDataAsset))
                return;
            
            CreateAnimationReferenceAssets(skeletonDataAsset);
            
            var assetPath = AssetDatabase.GetAssetPath(skeletonDataAsset);
            var assetFolderPath = assetPath.Replace(Path.GetFileName(assetPath), "");
            var animationAssetsPath = Path.Combine(assetFolderPath, "ReferenceAssets");
            var split = assetPath.Split('/');
            var prefabName = split[split.Length > 1 ? split.Length - 2 : 0];
            var isPlayer = prefabName.StartsWith("1");
            var prefabPath = Path.Combine(isPlayer ? PlayerPrefabPath : MonsterPrefabPath, $"{prefabName}.prefab");
            var skeletonAnimation =
                SpineEditorUtilities.EditorInstantiation.InstantiateSkeletonAnimation(skeletonDataAsset);
            skeletonAnimation.AnimationName = nameof(CharacterAnimation.Type.Idle);

            var gameObject = skeletonAnimation.gameObject;
            gameObject.name = prefabName;
            gameObject.layer = LayerMask.NameToLayer("Character");
            gameObject.transform.position = Position;
            gameObject.transform.localScale = LocalScale;

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingLayerName = "Character";

            var animatorControllerGuidArray = AssetDatabase.FindAssets(FindAssetFilter);
            if (animatorControllerGuidArray.Length == 0)
            {
                Object.DestroyImmediate(gameObject);
                throw new AssetNotFoundException($"AssetDatabase.FindAssets(\"{FindAssetFilter}\")");
            }

            var animatorControllerPath = AssetDatabase.GUIDToAssetPath(animatorControllerGuidArray[0]);
            var animator = gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorControllerPath);

            var controller = isPlayer
                ? gameObject.AddComponent<PlayerSpineController>()
                : gameObject.AddComponent<CharacterSpineController>();
            foreach (var animationType in CharacterAnimation.List)
            {
                assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (asset is null)
                {
                    switch (animationType)
                    {
                        case CharacterAnimation.Type.Appear:
                        case CharacterAnimation.Type.Standing:
                        case CharacterAnimation.Type.StandingToIdle:
                        case CharacterAnimation.Type.Win:
                        case CharacterAnimation.Type.Disappear:
                        case CharacterAnimation.Type.Greeting:
                        case CharacterAnimation.Type.Emotion:
                        case CharacterAnimation.Type.Attack:
                        case CharacterAnimation.Type.Run:
                        case CharacterAnimation.Type.Casting:
                        case CharacterAnimation.Type.Hit:
                        case CharacterAnimation.Type.Die:
                            assetPath = Path.Combine(animationAssetsPath,
                                $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        case CharacterAnimation.Type.Touch:
                        case CharacterAnimation.Type.CastingAttack:
                        case CharacterAnimation.Type.CriticalAttack:
                            assetPath = Path.Combine(animationAssetsPath,
                                $"{nameof(CharacterAnimation.Type.Attack)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        default:
                            Object.DestroyImmediate(gameObject);
                            throw new AssetNotFoundException(assetPath);
                    }

                    if (asset is null)
                    {
                        assetPath = Path.Combine(animationAssetsPath,
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
            
            if (File.Exists(prefabPath))
            {
                var boxCollider = controller.GetComponent<BoxCollider>();
                var sac = AssetDatabase.LoadAssetAtPath<SpineController>(prefabPath);
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

        private static bool ValidateForPlayerOrMonster(SkeletonDataAsset skeletonDataAsset)
        {
            var data = skeletonDataAsset.GetSkeletonData(false);
            var hud = data.FindBone("HUD");
            
            // todo: 커스터마이징 슬롯 검사.
            
            return !(hud is null);
        }
        
        private static bool ValidateForNPC(SkeletonDataAsset skeletonDataAsset)
        {
            return true;
        }

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
                typeof(AnimationReferenceAsset).GetField("animationName",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            if (nameField is null)
            {
                throw new NullReferenceException(
                    "typeof(AnimationReferenceAsset).GetField(\"animationName\", BindingFlags.NonPublic | BindingFlags.Instance);");
            }

            var skeletonDataAssetField = typeof(AnimationReferenceAsset).GetField("skeletonDataAsset",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (skeletonDataAssetField is null)
            {
                throw new NullReferenceException(
                    "typeof(AnimationReferenceAsset).GetField(\"skeletonDataAsset\", BindingFlags.NonPublic | BindingFlags.Instance);");
            }

            var skeletonData = skeletonDataAsset.GetSkeletonData(false);
            foreach (var animation in skeletonData.Animations)
            {
                var assetPath = $"{dataPath}/{SpineEditorUtilities.AssetUtility.GetPathSafeName(animation.Name)}.asset";
                var existingAsset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (existingAsset != null)
                    continue;

                AnimationReferenceAsset newAsset = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
                skeletonDataAssetField.SetValue(newAsset, skeletonDataAsset);
                nameField.SetValue(newAsset, animation.Name);
                AssetDatabase.CreateAsset(newAsset, assetPath);
            }

            var folderObject = AssetDatabase.LoadAssetAtPath(dataPath, typeof(UnityEngine.Object));
            if (folderObject != null)
            {
                Selection.activeObject = folderObject;
                EditorGUIUtility.PingObject(folderObject);
            }
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
                var skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataAssetPath);
                if (skeletonDataAsset is null)
                {
                    Debug.LogError($"Not Found SkeletonData from {skeletonDataAssetPath}");

                    continue;
                }

                CreateSpinePrefabInternal(skeletonDataAsset);
            }
        }
    }
}
