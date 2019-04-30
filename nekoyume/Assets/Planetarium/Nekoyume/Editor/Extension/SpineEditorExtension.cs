using System;
using System.IO;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Planetarium.Nekoyume.Unity.Editor.Extension
{
    public class SpineEditorExtension
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
            var split = assetPath.Split('/');
            var prefabName = split[split.Length > 1 ? split.Length - 2 : 0];
            var skeletonAnimation = SpineEditorUtilities.EditorInstantiation.InstantiateSkeletonAnimation(dataAsset);
            skeletonAnimation.AnimationName = "idle";

            var gameObject = skeletonAnimation.gameObject;
            gameObject.name = prefabName;
            gameObject.layer = LayerMask.NameToLayer("Character");

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingLayerName = "Character";

            // 아래의 프리펩을 addressable 로 설정하고, Character, Player, Monster label을 에디터에서 적용하는 방법을 아직 찾지 못함.
            PrefabUtility.SaveAsPrefabAsset(gameObject, Path.Combine(prefabPath, $"{prefabName}.prefab"));
            // AddressableAssetSettings.CreateAssetReference(Guid.NewGuid().ToString());
            // 찾는다면 이곳에서 일괄 처리 해야함.

            Object.DestroyImmediate(gameObject);
        }

        [MenuItem("Assets/Create/Spine Prefab", true)]
        public static bool CreateSpinePrefabValidation()
        {
            return Selection.activeObject.GetType() == typeof(SkeletonDataAsset);
        }
    }
}
