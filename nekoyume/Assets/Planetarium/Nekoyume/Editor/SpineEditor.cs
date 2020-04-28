using System;
using System.IO;
using System.Linq;
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
    // todo: Costume, NPC 로직 추가
    // todo: 사용자가 알기 쉽게 예외 상황 전부 알림 띄워주기.
    public static class SpineEditor
    {
        private const string FindAssetFilter = "CharacterAnimator t:AnimatorController";

        private const string CostumePrefabPath = "Assets/Resources/Character/Costume";
        private const string CostumeSpineRootPath = "Assets/AddressableAssets/Character/Costume";

        private const string MonsterPrefabPath = "Assets/Resources/Character/Monster";
        private const string MonsterSpineRootPath = "Assets/AddressableAssets/Character/Monster";

        private const string NPCPrefabPath = "Assets/Resources/Character/NPC";
        private const string NPCSpineRootPath = "Assets/AddressableAssets/Character/NPC";

        private const string PlayerPrefabPath = "Assets/Resources/Character/Player";
        private const string PlayerSpineRootPath = "Assets/AddressableAssets/Character/Player";

        private static readonly Vector3 Position = Vector3.zero;
        private static readonly Vector3 LocalScale = new Vector3(.64f, .64f, 1f);

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

            CreateSpinePrefabInternal(skeletonDataAsset);
        }

        // TODO: 코스튬 대응하기.
        // [MenuItem("Tools/9C/Create Spine Prefab(All Costume)", false, 0)]
        public static void CreateSpinePrefabAllOfCostume()
        {
            CreateSpinePrefabAllOfPath(CostumeSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Monster)", false, 0)]
        public static void CreateSpinePrefabAllOfMonster()
        {
            CreateSpinePrefabAllOfPath(MonsterSpineRootPath);
        }

        // TODO: NPC 대응하기.
        // [MenuItem("Tools/9C/Create Spine Prefab(All NPC)", false, 0)]
        public static void CreateSpinePrefabAllOfNPC()
        {
            CreateSpinePrefabAllOfPath(NPCSpineRootPath);
        }

        [MenuItem("Tools/9C/Create Spine Prefab(All Player)", false, 0)]
        public static void CreateSpinePrefabAllOfPlayer()
        {
            CreateSpinePrefabAllOfPath(PlayerSpineRootPath);
        }

        private static void CreateSpinePrefabInternal(SkeletonDataAsset skeletonDataAsset)
        {
            // todo: 플레이어나 몬스터가 아닌 Costume과 NPC도 이곳으로 들어올 수 있어야 해서, 아래 로직은 이 이전에 한 번 분기가 만들어져야 한다.
            if (!ValidateForPlayerOrMonster(skeletonDataAsset))
            {
                return;
            }

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
            // 지금은 예상 외의 애니메이션을 찾지 못하는 로직이다.
            // animationAssetsPath 하위에 있는 모든 것을 검사..?
            // 애초에 CreateAnimationReferenceAssets() 단계에서 검사할 수 있겠다.
            foreach (var animationType in CharacterAnimation.List)
            {
                assetPath = Path.Combine(animationAssetsPath, $"{animationType}.asset");
                var asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (asset is null)
                {
                    switch (animationType)
                    {
                        // todo: `CharacterAnimation.Type.Appear`와 `CharacterAnimation.Type.Disappear`는 없어질 예정.
                        case CharacterAnimation.Type.Appear:
                        case CharacterAnimation.Type.Disappear:
                        case CharacterAnimation.Type.Standing:
                        case CharacterAnimation.Type.StandingToIdle:
                        case CharacterAnimation.Type.Win:
                        case CharacterAnimation.Type.Greeting:
                        case CharacterAnimation.Type.Emotion:
                        case CharacterAnimation.Type.Attack:
                        case CharacterAnimation.Type.Run:
                        case CharacterAnimation.Type.Casting:
                        case CharacterAnimation.Type.Hit:
                        case CharacterAnimation.Type.Die:
                            assetPath = Path.Combine(
                                animationAssetsPath,
                                $"{nameof(CharacterAnimation.Type.Idle)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        case CharacterAnimation.Type.Touch:
                        case CharacterAnimation.Type.CastingAttack:
                        case CharacterAnimation.Type.CriticalAttack:
                            assetPath = Path.Combine(
                                animationAssetsPath,
                                $"{nameof(CharacterAnimation.Type.Attack)}.asset");
                            asset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                            break;
                        default:
                            Object.DestroyImmediate(gameObject);
                            throw new AssetNotFoundException(assetPath);
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

            // 헤어타입을 결정한다.
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

        private static bool ValidateForCostume(SkeletonDataAsset skeletonDataAsset)
        {
            return true;
        }

        private static bool ValidateForNPC(SkeletonDataAsset skeletonDataAsset)
        {
            return true;
        }

        // TODO: 플레이어와 몬스터 분리하기.
        private static bool ValidateForPlayerOrMonster(SkeletonDataAsset skeletonDataAsset)
        {
            var data = skeletonDataAsset.GetSkeletonData(false);
            var hud = data.FindBone("HUD");

            // todo: 플레이어의 경우만 커스터마이징 슬롯 검사.

            return !(hud is null);
        }

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
                var assetPath = $"{dataPath}/{SpineEditorUtilities.AssetUtility.GetPathSafeName(animation.Name)}.asset";
                var existingAsset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (!(existingAsset is null))
                {
                    continue;
                }

                AnimationReferenceAsset newAsset = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
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
