using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace NekoyumeEditor
{
    public class WidgetTextShadowEditor
    {
        [MenuItem("Tools/Nekoyume/Widget Text Shadow")]
        public static void WidgetTextShadow()
        {
            if (Selection.activeGameObject == null)
                return;

            var texts = Selection.activeGameObject.GetComponentsInChildren<Text>();

            bool useGraphicAlpha = true;

            Color blackColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color blueColor = new Color(0.04313726f, 0.08627451f, 0.2470588f, 1.0f);

            Vector2Int[] outlines = {
                new Vector2Int(1, 1),
            };

            Vector2Int[] shadows = {
                new Vector2Int(0, -1),
                new Vector2Int(0, -1)
            };

            foreach (var text in texts)
            {
                ClearEffects(text.gameObject);
            }
            foreach (var text in texts)
            {
                Color effectColor = blackColor;
                if (text.gameObject.transform.parent)
                {
                    var parentImage = text.gameObject.transform.parent.GetComponent<Image>();
                    if (parentImage)
                    {
                        bool isBlue = parentImage.sprite.name.ToLower().Contains("blue");
                        if (isBlue)
                        {
                            effectColor = blueColor;
                        }
                    }
                }

                for (int i = 0; i < outlines.Length; ++i)
                {
                    var outline = text.gameObject.AddComponent<Outline>();
                    outline.effectColor = effectColor;
                    outline.effectDistance = outlines[i];
                    outline.useGraphicAlpha = useGraphicAlpha;
                }
                for (int i = 0; i < shadows.Length; ++i)
                {
                    var shadow = text.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = effectColor;
                    shadow.effectDistance = shadows[i];
                    shadow.useGraphicAlpha = useGraphicAlpha;
                }
            }
        }

        private static void ClearEffects(GameObject go)
        {
            Shadow[] shadows = go.GetComponents<Shadow>();
            foreach (var shadow in shadows)
            {
                GameObject.DestroyImmediate(shadow);
            }
            Outline[] outlines = go.GetComponents<Outline>();
            foreach (var outline in outlines)
            {
                GameObject.DestroyImmediate(outline);
            }
        }
    }
}
