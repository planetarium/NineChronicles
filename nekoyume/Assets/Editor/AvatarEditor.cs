using UnityEditor;
using UnityEngine;

namespace Anima2D
{
    public class AvatarEditor
    {
        [MenuItem("Tools/Nekoyume/Create Avatar")]
        public static void CreateAvatar()
        {
            if (Selection.objects.Length == 0)
                return;

            var root = new GameObject("Avatar");
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(root.transform);
            var spine = new GameObject("Spine");
            spine.transform.SetParent(root.transform);

            foreach (Object obj in Selection.objects)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
                var part = new GameObject(sprite.name);
                part.transform.SetParent(mesh.transform);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
            }
        }
    }
}
