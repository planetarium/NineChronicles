using Nekoyume.Helper;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class TextEditor
    {
        private const string NumberTextPrefabPath = "Assets/AddressableAssets/UI/Module/NumberText.prefab";
        
        [MenuItem("GameObject/Nekoyume/UI - Add Text(Number)", true)]
        public static bool RectTransformValidation()
        {
            return !ReferenceEquals(Selection.activeGameObject, null) &&
                   !ReferenceEquals(Selection.activeGameObject.GetComponent<RectTransform>(), null);
        }
        
        [MenuItem("GameObject/Nekoyume/UI - Add Text(Number)", false, 10000)]
        public static void CreateUINumberText()
        {
            CreateUIText(NumberTextPrefabPath);
        }

        private static void CreateUIText(string prefabPath)
        {
            if (ReferenceEquals(Selection.activeGameObject, null))
            {
                return;
            }

            var parent = Selection.activeGameObject.GetComponent<RectTransform>();
            if (ReferenceEquals(parent, null))
            {
                return;
            }
            
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Object.Instantiate(prefab, parent);
        }
        
        [MenuItem("CONTEXT/TextMeshProUGUI/Set Text(Static)", false, 10000)]
        public static void SetUITextStatic(MenuCommand command)
        {
            var text = (TextMeshProUGUI) command.context;
            text.color = ColorHelper.HexToColorRGB("A2B4D6");
        }
        
        [MenuItem("CONTEXT/TextMeshProUGUI/Set Text(Default)", false, 10000)]
        public static void SetUITextDefault(MenuCommand command)
        {
            var text = (TextMeshProUGUI) command.context;
            text.color = ColorHelper.HexToColorRGB("FFF9DD");
        }
        
        [MenuItem("CONTEXT/TextMeshProUGUI/Set Text(Strong)", false, 10000)]
        public static void SetUITextStrong(MenuCommand command)
        {
            var text = (TextMeshProUGUI) command.context;
            text.color = ColorHelper.HexToColorRGB("00E607");
        }
        
        [MenuItem("CONTEXT/TextMeshProUGUI/Set Text(Danger)", false, 10000)]
        public static void SetUITextDanger(MenuCommand command)
        {
            var text = (TextMeshProUGUI) command.context;
            text.color = ColorHelper.HexToColorRGB("FF2116");
        }
        
        [MenuItem("CONTEXT/TextMeshProUGUI/Set Text(EXP)", false, 10000)]
        public static void SetUITextEXP(MenuCommand command)
        {
            var text = (TextMeshProUGUI) command.context;
            text.color = ColorHelper.HexToColorRGB("3CA6FF");
        }
    }
}
