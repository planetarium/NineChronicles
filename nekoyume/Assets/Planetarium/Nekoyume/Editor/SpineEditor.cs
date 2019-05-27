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
    public class SpineEditor
    {
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

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingLayerName = "Character";

            var findAssetFilter = "CharacterAnimator t:AnimatorController";
            var animatorControllerGuidArray = AssetDatabase.FindAssets(findAssetFilter);
            if (animatorControllerGuidArray.Length == 0)
            {
                Object.DestroyImmediate(gameObject);
                throw new AssetNotFoundException($"AssetDatabase.FindAssets(\"{findAssetFilter}\")");
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

        [MenuItem("Assets/Create/Spine Prefab", true)]
        public static bool CreateSpinePrefabValidation()
        {
            return Selection.activeObject.GetType() == typeof(SkeletonDataAsset);
        }
    }
}
