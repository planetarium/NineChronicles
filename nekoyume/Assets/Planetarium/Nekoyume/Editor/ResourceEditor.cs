using System.Text;
using Nekoyume.Game.VFX.Skill;
using UnityEditor;
using UnityEngine;

public static class ResourceEditor
{
    private const string VFXPrefabPath = "Assets/Resources/VFX/";

    [MenuItem("Tools/Resource/Set All Character VFX Prefab Rendering Layer")]
    public static void SetAllCharacterVFXPrefabRenderingLayer()
    {
        // TODO: 일단 현재 프로젝트의 리소스 구조대로 스크립트를 짜긴 했는데, 리소스가 구분이 안되는 느낌이라 경로부터 수정해야할 것 같음
        var vfxAssetPath = AssetDatabase.FindAssets("t:GameObject", new[] { VFXPrefabPath });

        var sb = new StringBuilder();
        sb.AppendLine("Set Character VFX Prefab Rendering Layer:");
        foreach (var assetPath in vfxAssetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(assetPath));
            if (asset == null)
            {
                continue;
            }

            // SkillVFX & BuffVFX 상대로만 레이어 처리
            if (!asset.TryGetComponent<SkillVFX>(out _) && !asset.TryGetComponent<BuffVFX>(out _))
            {
                continue;
            }

            sb.AppendLine(asset.name);
            var vfxRenderers = asset.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (var vfxRenderer in vfxRenderers)
            {
                vfxRenderer.sortingLayerName = "CharacterVFX";
            }

            if (asset.TryGetComponent<SkillVFX>(out var skillVFX))
            {
                foreach (var backGroundVfx in skillVFX.BackgroundParticleSystems)
                {
                    backGroundVfx.sortingLayerName = "InGameBackgroundVFX";
                }
            }

            if (asset.TryGetComponent<BuffVFX>(out var buffVFX))
            {
                foreach (var backGroundVfx in buffVFX.BackgroundParticleSystems)
                {
                    backGroundVfx.sortingLayerName = "InGameBackgroundVFX";
                }
            }

            EditorUtility.SetDirty(asset);
        }
        Debug.Log(sb.ToString());
    }
}
