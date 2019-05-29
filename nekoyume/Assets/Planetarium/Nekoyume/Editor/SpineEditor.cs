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
        private static readonly Vector3 PlayerPosition = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 EnemyPosition = Vector3.zero;
        private static readonly Vector3 CharacterLocalScale = new Vector3(.64f, .64f, 1f);
        private const string FindAssetFilter = "CharacterAnimator t:AnimatorController";

        [MenuItem("Assets/Create/Spine Prefab", true)]
        public static bool CreateSpinePrefabValidation()
        {
            return Selection.activeObject is SkeletonDataAsset;
        }
      
        [MenuItem("Assets/Create/Spine Prefab", false, 10000)]
        public static void CreateSpinePrefab()
        {
            var dataAsset = Selection.activeObject as SkeletonDataAsset;
            if (ReferenceEquals(dataAsset, null))
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var prefabPath = assetPath.Replace(Path.GetFileName(assetPath), "");
            var animationAssetsPath = Path.Combine(prefabPath, "ReferenceAssets");
            var split = assetPath.Split('/');
            var prefabName = split[split.Length > 1 ? split.Length - 2 : 0];
            var skeletonAnimation = SpineEditorUtilities.EditorInstantiation.InstantiateSkeletonAnimation(dataAsset);
            skeletonAnimation.AnimationName = nameof(CharacterAnimation.Type.Idle);

            var gameObject = skeletonAnimation.gameObject;
            gameObject.name = prefabName;
            gameObject.layer = LayerMask.NameToLayer("Character");
            gameObject.transform.position = prefabName.StartsWith("1") ? PlayerPosition : EnemyPosition;
            gameObject.transform.localScale = CharacterLocalScale;

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
                    if (animationType == CharacterAnimation.Type.Appear ||
                        animationType == CharacterAnimation.Type.Disappear)
                    {
                        assetPath = Path.Combine(animationAssetsPath, $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                        asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
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

            var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, Path.Combine(prefabPath, $"{prefabName}.prefab"));

            Object.DestroyImmediate(gameObject);
            Selection.activeObject = prefab;
        }
    }
}
