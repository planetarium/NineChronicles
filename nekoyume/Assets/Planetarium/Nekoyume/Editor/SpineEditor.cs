using System.IO;
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
    public static class SpineEditor
    {
        private const string FindAssetFilter = "CharacterAnimator t:AnimatorController";
        private const string PlayerPrefabPath = "Assets/Resources/Character/Player";
        private const string MonsterPrefabPath = "Assets/Resources/Character/Monster";
        
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
            var dataAsset = Selection.activeObject as SkeletonDataAsset;
            if (ReferenceEquals(dataAsset, null))
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var assetFolderPath = assetPath.Replace(Path.GetFileName(assetPath), "");
            var animationAssetsPath = Path.Combine(assetFolderPath, "ReferenceAssets");
            var split = assetPath.Split('/');
            var prefabName = split[split.Length > 1 ? split.Length - 2 : 0];
            var isPlayer = prefabName.StartsWith("1");
            var prefabPath = Path.Combine(isPlayer ? PlayerPrefabPath : MonsterPrefabPath, $"{prefabName}.prefab");
            var skeletonAnimation = SpineEditorUtilities.EditorInstantiation.InstantiateSkeletonAnimation(dataAsset);
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
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorControllerPath);

            var controller = gameObject.AddComponent<SkeletonAnimationController>();
            foreach (var animationType in CharacterAnimation.List)
            {
                assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (ReferenceEquals(asset, null))
                {
                    switch (animationType)
                    {
                        case CharacterAnimation.Type.Appear:
                        case CharacterAnimation.Type.Disappear:
                            assetPath = Path.Combine(animationAssetsPath, $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        case CharacterAnimation.Type.CastingAttack:
                        case CharacterAnimation.Type.CriticalAttack:
                            assetPath = Path.Combine(animationAssetsPath, $"{nameof(CharacterAnimation.Type.Attack)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        default:
                            Object.DestroyImmediate(gameObject);
                            throw new AssetNotFoundException(assetPath);
                    }

                    if (ReferenceEquals(asset, null))
                    {
                        Object.DestroyImmediate(gameObject);
                        throw new AssetNotFoundException(assetPath);
                    }
                }
                
                controller.statesAndAnimations.Add(
                    new SkeletonAnimationController.StateNameToAnimationReference
                    {
                        stateName = animationType.ToString(),
                        animation = asset
                    });
            }

            if (File.Exists(prefabPath))
            {
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
                throw new FailedToInstantiateGameObjectException(prefabPath);
            }
        }
    }
}
