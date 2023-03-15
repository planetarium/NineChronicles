using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bencodex.Types;
using ModelViewer.Runtime;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;

namespace ModelViewer.Editor
{
    public class ModelViewerWindow : EditorWindow
    {
        private readonly struct TypeModel
        {
            private const BindingFlags Flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            public readonly Type Type;
            public readonly FieldInfo[] FieldInfos;
            public readonly PropertyInfo[] PropertyInfos;

            public TypeModel(Type type)
            {
                Type = type;
                FieldInfos = type.GetFields(Flags);
                PropertyInfos = type.GetProperties(Flags);
            }
        }

        private readonly struct TypeViewModel
        {
            public readonly GUIContent Content;
            public readonly RowViewModel[] Fields;
            public readonly RowViewModel[] Properties;
            public readonly object Object;
            public readonly IValue SerializedObject;

            public TypeViewModel(TypeModel typeModel)
            {
                Content = new GUIContent(typeModel.Type.FullName);
                Fields = typeModel.FieldInfos
                    .Select(fi => new RowViewModel(fi))
                    .ToArray();
                Properties = typeModel.PropertyInfos
                    .Select(pi => new RowViewModel(pi))
                    .ToArray();
                Object = ModelFactory.Create(typeModel.Type);
                SerializedObject = (IValue)Object.GetType()
                    .GetMethod("Serialize")?
                    .Invoke(Object, null);
            }
        }

        private readonly struct RowViewModel
        {
            public readonly string TypeFullName;
            public readonly string Name;

            public RowViewModel(FieldInfo fieldInfo)
            {
                TypeFullName = fieldInfo.FieldType.FullName;
                Name = fieldInfo.Name;
            }

            public RowViewModel(PropertyInfo propertyInfo)
            {
                TypeFullName = propertyInfo.PropertyType.FullName;
                Name = propertyInfo.Name;
            }
        }

        private const string Lib9CModelNamespace = "Lib9c.Model";
        private const string NekoyumeModelNamespace = "Nekoyume.Model";

        private static readonly Dictionary<Type, TypeModel> ModelCache = new();

        private static List<TypeViewModel> _viewModelCache = new();

        private int _selectedIndex;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Lib9c/Model Viewer")]
        public static void ShowWindow() =>
            GetWindow<ModelViewerWindow>("Model Viewer", true).Show();

        private void OnEnable()
        {
            _selectedIndex = 0;
            _scrollPos = Vector2.zero;
        }

        private static void GenerateTypes()
        {
            ModelCache.Clear();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .ToArray();
            CacheModels(types, Lib9CModelNamespace, ModelCache, _viewModelCache);
            CacheModels(types, NekoyumeModelNamespace, ModelCache, _viewModelCache);
            _viewModelCache = _viewModelCache
                .OrderBy(e => e.Content.text)
                .ToList();
            Debug.Log($"[ModelViewer] Cached {ModelCache.Count} models.");
        }

        private static void CacheModels(
            IEnumerable<Type> types,
            string namespaceName,
            IDictionary<Type, TypeModel> modelCache,
            ICollection<TypeViewModel> viewModelCache)
        {
            foreach (var type in types
                         .Where(type =>
                             (type.Namespace?.Contains(namespaceName) ?? false) &&
                             !type.IsAbstract &&
                             !type.IsEnum &&
                             !type.IsInterface &&
                             !type.GetCustomAttributes().Any(attr =>
                                 attr is ObsoleteAttribute
                                     or CompilerGeneratedAttribute)))
            {
                // NOTE: Skip types that does not have a `IValue Serialize()` method.
                var methodInfo = type.GetMethod("Serialize");
                if (methodInfo is null ||
                    methodInfo.GetParameters().Length != 0 ||
                    methodInfo.ReturnType != typeof(IValue))
                {
                    continue;
                }

                // NOTE: Skip `LazyState<,>` types because it does not stored in the state directly.
                if (type.IsGenericType &&
                    typeof(LazyState<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))
                {
                    continue;
                }

                var typeModel = new TypeModel(type);
                var typeViewModel = new TypeViewModel(typeModel);
                modelCache[type] = typeModel;
                viewModelCache.Add(typeViewModel);
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Generate Types"))
            {
                OnEnable();
                GenerateTypes();
            }

            _selectedIndex = EditorGUILayout.Popup(
                new GUIContent("Type"),
                _selectedIndex,
                _viewModelCache
                    .Select(vm => vm.Content)
                    .ToArray());
            if (_selectedIndex < 0 ||
                _selectedIndex >= _viewModelCache.Count)
            {
                return;
            }

            EditorStyles.label.richText = true;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            var selectedVm = _viewModelCache[_selectedIndex];
            EditorGUILayout.Space();
            GUILayout.Label("Fields", EditorStyles.boldLabel);
            foreach (var vm in selectedVm.Fields)
            {
                GUILayout.Label($"<b>{vm.Name}</b>: {vm.TypeFullName}", EditorStyles.label);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Properties", EditorStyles.boldLabel);
            foreach (var vm in selectedVm.Properties)
            {
                GUILayout.Label($"<b>{vm.Name}</b>: {vm.TypeFullName}", EditorStyles.label);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Objects", EditorStyles.boldLabel);
            GUILayout.Label(selectedVm.Object?.ToString() ?? "null");
            GUILayout.Label(selectedVm.SerializedObject.Inspect(true));

            EditorGUILayout.EndScrollView();
            EditorStyles.label.richText = false;
        }
    }
}
