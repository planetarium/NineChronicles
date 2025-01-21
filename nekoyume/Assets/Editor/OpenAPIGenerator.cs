using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using UnityEngine.Networking;
using System.Text.Json.Nodes;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;

namespace Nekoyume
{
    public class GeneratedApiTester : EditorWindow
    {
        private object clientInstance;
        private string url = "";
        private string responseMessage;
        private string className = "";
        private List<string> methodNames = new();
        private List<MethodInfo> methodInfos = new();
        private int selectedMethodIndex = 0;
        private List<object> methodParameters = new();
        private Dictionary<string, object> customClassValues = new();
        private Vector2 scrollPosition;

        public static void ShowWindow(string startURl, string className)
        {
            var window = GetWindow(typeof(GeneratedApiTester)) as GeneratedApiTester;
            window.methodInfos.Clear();
            window.methodNames.Clear();
            window.selectedMethodIndex = 0;
            window.methodParameters.Clear();
            window.className = className;
            window.url = startURl;
        }

        private void OnGUI()
        {
            GUILayout.Label("Generated API Tester", EditorStyles.boldLabel);

            url = EditorGUILayout.TextField("Service URL: ", url);
            className = EditorGUILayout.TextField("Class Name: ", className);

            if (GUILayout.Button("Create Client Instance"))
            {
                selectedMethodIndex = 0;
                CreateClientInstance();
                LoadMethodsFromClientType();
            }

            if (clientInstance != null)
            {
                var newSelectedMethodIndex = EditorGUILayout.Popup("Select Method", selectedMethodIndex, methodNames.ToArray());

                if (newSelectedMethodIndex != selectedMethodIndex)
                {
                    selectedMethodIndex = newSelectedMethodIndex;
                    LoadMethodsFromClientType();
                    LoadMethodParameters();
                }

                DrawMethodParameters();

                if (GUILayout.Button("Invoke Selected Method"))
                {
                    InvokeSelectedMethod();
                    scrollPosition = Vector2.zero;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(responseMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void LoadMethodParameters()
        {
            methodParameters.Clear();

            var parameters = methodInfos[selectedMethodIndex].GetParameters();
            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(string))
                {
                    methodParameters.Add("");
                }
                else if (param.ParameterType.IsValueType)
                {
                    methodParameters.Add(Activator.CreateInstance(param.ParameterType));
                }
                else if (IsCallback(param.ParameterType))
                {
                    var callbackType = param.ParameterType.GetGenericArguments()[0];
                    var callback = CreateDynamicCallback(callbackType);
                    methodParameters.Add(callback);
                }
                else
                {
                    methodParameters.Add(null);
                }
            }
        }

        private void DrawMethodParameters()
        {
            var parameters = methodInfos[selectedMethodIndex].GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].Name;
                var paramType = parameters[i].ParameterType;

                if (paramType == typeof(string))
                {
                    methodParameters[i] = EditorGUILayout.TextField(paramName + " (string)", (string)methodParameters[i]);
                }
                else if (paramType == typeof(int))
                {
                    methodParameters[i] = EditorGUILayout.IntField(paramName + " (int)", (int)methodParameters[i]);
                }
                else if (paramType == typeof(Int64))
                {
                    methodParameters[i] = EditorGUILayout.LongField(paramName + " (Int64)", (Int64)methodParameters[i]);
                }
                else if (paramType == typeof(float))
                {
                    methodParameters[i] = EditorGUILayout.FloatField(paramName + " (float)", (float)methodParameters[i]);
                }
                else if (IsCallback(paramType))
                {
                }
                else if (IsCustomClass(paramType))
                {
                    DrawCustomClassFields(paramName, paramType, out var classInstance);
                    methodParameters[i] = classInstance;
                }
                else if (paramType.IsEnum)
                {
                    methodParameters[i] = EditorGUILayout.EnumPopup(paramName, (Enum)methodParameters[i]);
                }
                else
                {
                    EditorGUILayout.LabelField($"Parameter {paramName} of type {paramType.Name} not supported.");
                }
            }
        }

        private void DrawCustomClassFields(string paramName, Type paramType, out object classIntance)
        {
            var fields = paramType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            EditorGUILayout.LabelField($"Editing {paramName} ({paramType.Name}):");
            EditorGUI.indentLevel++;

            object customClassInstance;
            if (!customClassValues.TryGetValue(paramName, out customClassInstance))
            {
                customClassInstance = Activator.CreateInstance(paramType);
                customClassValues[paramName] = customClassInstance;
            }

            classIntance = customClassInstance;

            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    UpdatePropertyValue(customClassInstance, prop);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void UpdatePropertyValue(object instance, PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType);
            var isNullable = underlyingType != null;
            if (isNullable)
            {
                propertyType = underlyingType;
            }

            var propertyValue = property.GetValue(instance);

            if (propertyType == typeof(int))
            {
                propertyValue = isNullable ? HandleNullableInt((int?)propertyValue, property.Name) : EditorGUILayout.IntField(property.Name + " (int)", (int)propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                propertyValue = EditorGUILayout.TextField(property.Name + " (string)", (string)propertyValue);
            }
            else if (propertyType == typeof(float))
            {
                propertyValue = isNullable ? HandleNullableFloat((float?)propertyValue, property.Name) : EditorGUILayout.FloatField(property.Name + " (float)", (float)propertyValue);
            }
            else if (propertyType == typeof(bool))
            {
                propertyValue = isNullable ? HandleNullableBool((bool?)propertyValue, property.Name) : EditorGUILayout.Toggle(property.Name + " (bool)", (bool)propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                if (propertyValue == null)
                {
                    var enumValues = Enum.GetValues(propertyType);
                    propertyValue = enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                }
                else
                {
                    propertyValue = EditorGUILayout.EnumPopup(property.Name, (Enum)propertyValue);
                }
            }

            property.SetValue(instance, propertyValue);
        }

        private int? HandleNullableInt(int? value, string name)
        {
            if (!value.HasValue)
            {
                value = 0;
            }

            value = EditorGUILayout.IntField(name + " (int?)", value.Value);
            return value;
        }

        private float? HandleNullableFloat(float? value, string name)
        {
            if (!value.HasValue)
            {
                value = 0.0f;
            }

            value = EditorGUILayout.FloatField(name + " (float?)", value.Value);
            return value;
        }

        private bool? HandleNullableBool(bool? value, string name)
        {
            if (value.HasValue)
            {
                value = false;
            }

            value = EditorGUILayout.Toggle(name + " (bool?)", value.Value);
            return value;
        }

        private bool IsCustomClass(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        private static Type FindTypeInAllAssemblies(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == className && HasField(type, "Url", typeof(string)))
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static bool HasField(Type type, string fieldName, Type fieldType = null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fieldType == null)
            {
                return field != null;
            }

            return field != null && field.FieldType == fieldType;
        }

        private void CreateClientInstance()
        {
            var clientType = FindTypeInAllAssemblies(className);

            if (clientType != null)
            {
                clientInstance = Activator.CreateInstance(clientType, url);
                responseMessage = "Client instance created!";
            }
            else
            {
                responseMessage = $"Failed to find {className} type.";
            }
        }

        private void LoadMethodsFromClientType()
        {
            if (clientInstance != null)
            {
                methodNames.Clear();
                methodInfos.Clear();
                customClassValues.Clear();
                var methods = clientInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    methodNames.Add(method.Name);
                    methodInfos.Add(method);
                }

                responseMessage = $"Loaded {methodNames.Count} methods!";
            }
            else
            {
                responseMessage = "Client instance not created.";
            }
        }

        private void InvokeSelectedMethod()
        {
            var method = methodInfos[selectedMethodIndex];

            try
            {
                responseMessage = "Requesting~";
                Repaint();

                var result = method.Invoke(clientInstance, methodParameters.ToArray());
                if (result is Task task)
                {
                    WaitForTaskCompletion(task);
                }
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }

            Repaint();
        }

        private bool IsCallback(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            return type.GetGenericTypeDefinition() == typeof(Action<>);
        }

        private Delegate CreateDynamicCallback(Type callbackType)
        {
            var method = GetType().GetMethod(nameof(GenericCallback), BindingFlags.NonPublic | BindingFlags.Instance);
            var generic = method.MakeGenericMethod(callbackType);
            return (Delegate)generic.CreateDelegate(typeof(Action<>).MakeGenericType(callbackType), this);
        }

        private async void WaitForTaskCompletion(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                responseMessage = $"Task Error: {ex.Message}";
                Repaint();
            }
        }

        private void GenericCallback<T>(T obj)
        {
            if (obj is string || obj is int || obj is float || obj is bool)
            {
                responseMessage = obj.ToString();
            }
            else
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var serializedData = JsonSerializer.Serialize(obj, options);
                responseMessage = $"{serializedData}";
            }
            Repaint();
        }
    }


    public class OpenApiGenerator : EditorWindow
    {
        private const string _outputDir = "Assets/_Scripts/ApiClient/GeneratedApi";
        private const int _timeOut = 10;

        private static string _downloadJsonUrl;
        private static string _className;
        private static List<KeyValuePair<string, string>> _dataList = new();
        private Vector2 _scrollPosition;

        private static HashSet<string> inValidEnumTypeList = new HashSet<string>();

        [MenuItem("Tools/Generate OpenAPI Class")]
        private static void Init()
        {
            RefreshGeneratedInfo();
            var window = GetWindow(typeof(OpenApiGenerator));
            window.Show();
        }
        private void OnGUI()
        {
            RefreshGeneratedInfo();
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _downloadJsonUrl = EditorGUILayout.TextField("DownloadJsonURL: ", _downloadJsonUrl);
            _className = EditorGUILayout.TextField("ClassName: ", _className);

            if (GUILayout.Button("Create"))
            {
                GenerateOpenApiClass(_downloadJsonUrl, _className);
                AssetDatabase.Refresh();
                RefreshGeneratedInfo();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("RefreshAll"))
            {
                foreach (var item in _dataList)
                {
                    GenerateOpenApiClass(item.Value, item.Key);
                }
                AssetDatabase.Refresh();
                RefreshGeneratedInfo();
            }

            GUILayout.Space(10);

            if (_dataList != null)
            {
                GUILayout.BeginVertical();
                foreach (var data in _dataList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(data.Key, data.Value));
                    if (GUILayout.Button("Apply"))
                    {
                        _className = data.Key;
                        _downloadJsonUrl = data.Value;
                    }

                    if (GUILayout.Button("TesterWindow"))
                    {
                        var baseUrl = data.Value;
                        var comIndex = baseUrl.IndexOf(".com");
                        if (comIndex >= 0)
                        {
                            baseUrl = baseUrl.Substring(0, comIndex + 4);
                        }
                        var swaggerIndex = baseUrl.IndexOf("/swagger");
                        if (swaggerIndex >= 0)
                        {
                            baseUrl = baseUrl.Substring(0, swaggerIndex);
                        }
                        GeneratedApiTester.ShowWindow(baseUrl, data.Key);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public static void GenerateOpenApiClass(string url, string className)
        {
            var apiUrl = url;

            var jsonSpec = DownloadOpenApiSpec(apiUrl);
            if (string.IsNullOrEmpty(jsonSpec))
            {
                Debug.LogError("Failed to download OpenAPI spec.");
                return;
            }

            var generatedCode = GenerateCSharpFromOpenApiSpec(jsonSpec, className, url);
            if (string.IsNullOrEmpty(generatedCode))
            {
                Debug.LogError("Failed to generate C# code from OpenAPI spec.");
                return;
            }

            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }

            File.WriteAllText(Path.Combine(_outputDir, $"{className}.cs"), generatedCode);
        }

        private static string DownloadOpenApiSpec(string url)
        {
            using (var www = UnityWebRequest.Get(url))
            {
                www.timeout = 5;
                www.SendWebRequest();
                while (!www.isDone)
                {
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{url} Failed to download: {www.error}");
                    return null;
                }

                return www.downloadHandler.text;
            }
        }

        private static string GenerateCSharpFromOpenApiSpec(string jsonSpec, string rootClassName, string url)
        {
            inValidEnumTypeList.Clear();

            var sb = new StringBuilder();
            var rootNode = JsonNode.Parse(jsonSpec);

            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     This code was generated by a tool.");
            sb.AppendLine("//     Do not modify the contents of this file directly.");
            sb.AppendLine("//     Changes might be overwritten the next time the code is generated.");
            sb.AppendLine("//     Source URL: " + url);
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("//------------------------------------------------------------------------------");

            sb.AppendLine();
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using UnityEngine.Networking;");
            sb.AppendLine("using Cysharp.Threading.Tasks;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Linq;");

            sb.AppendLine();

            // 네임스페이스 추가
            sb.AppendLine($"namespace GeneratedApiNamespace.{rootClassName}{{");

            if (rootNode["components"]?["schemas"] is JsonObject schemas)
            {
                foreach (var schema in schemas)
                {
                    var className = schema.Key;
                    var classDescription = schema.Value["description"]?.ToString();

                    // Add class summary if description exists
                    if (!string.IsNullOrEmpty(classDescription))
                    {
                        sb.AppendLine("    /// <summary>");
                        string processedDescription = classDescription
                            .Replace("\r\n", "\r\n    /// ")
                            .Replace("\n", "\n    /// ");
                        sb.AppendLine($"    /// <para>{processedDescription}</para>");
                        sb.AppendLine("    /// </summary>");
                    }

                    if (schema.Value["enum"] is JsonArray enumValues)
                    {
                        sb.AppendLine($"    [JsonConverter(typeof({schema.Key}TypeConverter))]");
                        sb.AppendLine($"    public enum {schema.Key}");
                        sb.AppendLine("    {");

                        var enumMapping = new Dictionary<string, string>();

                        var enumType = schema.Value["type"]?.ToString();
                        if (enumType != "string" && classDescription != null)
                        {
                            foreach (var line in classDescription.Split('\n'))
                            {
                                if (line.Contains(":"))
                                {
                                    var parts = line.Trim().Split(':');
                                    if (parts.Length == 2)
                                    {
                                        var name = parts[1];
                                        var pattern = "`(.*?)`";
                                        var match = Regex.Match(name, pattern);
                                        if (match.Success)
                                        {
                                            name = match.Groups[1].Value.Trim('`');
                                        }

                                        enumMapping[parts[0].Trim('*').Trim().Replace("- **", string.Empty).TrimEnd('"')] = name;
                                    }
                                }
                            }
                        }

                        var invalidEnumMapping = new Dictionary<string, string>();
                        foreach (var enumValue in enumValues)
                        {
                            var enumVal = enumValue.ToString();
                            if (!IsValidEnumName(enumVal) || IsCSharpKeyword(enumVal))
                            {
                                var validEnumVal = MakeValidEnumName(enumVal);
                                invalidEnumMapping[enumVal] = validEnumVal;
                                enumVal = validEnumVal;
                            }

                            if (enumMapping.TryGetValue(enumValue.ToString(), out string enumName))
                            {
                                // 매핑이 있는 경우 숫자 타입으로 가정
                                sb.AppendLine($"        {enumName} = {enumValue},");
                                invalidEnumMapping[enumValue.ToString()] = enumName;
                            }
                            else
                            {
                                // 매핑이 없는 경우 일반적으로 처리
                                sb.AppendLine($"        {enumVal},");
                            }
                        }

                        if (invalidEnumMapping.Count > 0)
                        {
                            inValidEnumTypeList.Add(schema.Key);
                        }

                        sb.AppendLine("    }");
                        sb.AppendLine();

                        sb.AppendLine($"    public class {schema.Key}TypeConverter : JsonConverter<{schema.Key}>");
                        sb.AppendLine("    {");
                        sb.AppendLine("        public static readonly Dictionary<string, string> InvalidEnumMapping = new Dictionary<string, string>");
                        sb.AppendLine("        {");
                        foreach (var kvp in invalidEnumMapping)
                        {
                            sb.AppendLine($"            {{ \"{kvp.Key}\", \"{kvp.Value}\" }},");
                        }
                        sb.AppendLine("        };");
                        //todo : 성능개선 필요시 역방향 매핑 추가

                        sb.AppendLine("        public override " + schema.Key + " Read(");
                        sb.AppendLine("            ref Utf8JsonReader reader,");
                        sb.AppendLine("            Type typeToConvert,");
                        sb.AppendLine("            JsonSerializerOptions options)");
                        sb.AppendLine("        {");
                        sb.AppendLine("            return reader.TokenType switch");
                        sb.AppendLine("            {");
                        sb.AppendLine("                JsonTokenType.Number => (" + schema.Key + ")reader.GetInt32(),");
                        sb.AppendLine("                JsonTokenType.String => Enum.Parse<" + schema.Key + ">(InvalidEnumMapping.TryGetValue(reader.GetString(), out var validName) ? validName : reader.GetString()),");
                        sb.AppendLine("                _ => throw new JsonException(");
                        sb.AppendLine("                    $\"Expected token type to be {string.Join(\" or \", new[] { JsonTokenType.Number, JsonTokenType.String })} but got {reader.TokenType}\")");
                        sb.AppendLine("            };");
                        sb.AppendLine("        }");

                        sb.AppendLine("        public override void Write(");
                        sb.AppendLine("            Utf8JsonWriter writer,");
                        sb.AppendLine($"            {schema.Key} value,");
                        sb.AppendLine("            JsonSerializerOptions options)");
                        sb.AppendLine("        {");
                        if (enumType == "string")
                        {
                            sb.AppendLine("            var enumString = value.ToString();");
                            sb.AppendLine("            if (InvalidEnumMapping.ContainsValue(enumString))");
                            sb.AppendLine("            {");
                            sb.AppendLine("                enumString = InvalidEnumMapping.First(kvp => kvp.Value == enumString).Key;");
                            sb.AppendLine("            }");
                            sb.AppendLine("            writer.WriteStringValue(enumString);");
                        }
                        else if (enumType == "integer")
                        {
                            sb.AppendLine("            writer.WriteNumberValue((int)value);");
                        }
                        else
                        {
                            sb.AppendLine("            var enumString = value.ToString();");
                            sb.AppendLine("            if (InvalidEnumMapping.ContainsValue(enumString))");
                            sb.AppendLine("            {");
                            sb.AppendLine("                enumString = InvalidEnumMapping.First(kvp => kvp.Value == enumString).Key;");
                            sb.AppendLine("            }");
                            sb.AppendLine("            if (int.TryParse(enumString, out var intValue))");
                            sb.AppendLine("            {");
                            sb.AppendLine("                writer.WriteNumberValue(intValue);");
                            sb.AppendLine("            }");
                            sb.AppendLine("            else");
                            sb.AppendLine("            {");
                            sb.AppendLine("                writer.WriteStringValue(enumString);");
                            sb.AppendLine("            }");
                        }
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine();

                        continue;
                    }

                    sb.AppendLine($"    public class {className}");
                    sb.AppendLine("    {");

                    if (schema.Value["properties"] is JsonObject properties)
                    {
                        foreach (var property in properties)
                        {
                            var propName = property.Key;
                            var propertyDescription = property.Value["description"]?.ToString();

                            // Add property summary if description exists
                            if (!string.IsNullOrEmpty(propertyDescription))
                            {
                                sb.AppendLine("        /// <summary>");
                                string processedDescription = propertyDescription
                                    .Replace("\r\n", "\r\n    /// ")
                                    .Replace("\n", "\n    /// ");
                                sb.AppendLine($"        /// <para>{processedDescription}</para>");
                                sb.AppendLine("        /// </summary>");
                            }

                            var propType = ConvertToCSharpType(property.Value);

                            if (property.Value["type"]?.ToString() == "array")
                            {
                                propType = $"List<{ConvertToCSharpType(property.Value["items"])}>";
                            }

                            var splited = propName.Split("_");
                            for (var i = 0; i < splited.Length; i++)
                            {
                                splited[i] = char.ToUpper(splited[i][0]) + splited[i].Substring(1);
                            }

                            var formattedPropName = string.Concat(splited);
                            sb.AppendLine($"        [JsonPropertyName(\"{propName}\")]");
                            sb.AppendLine($"        public {propType} {formattedPropName} {{ get; set; }}");
                        }
                    }

                    sb.AppendLine("    }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine($"public class {rootClassName}");
            sb.AppendLine("{");
            sb.AppendLine("    private string Url;");
            sb.AppendLine();
            sb.AppendLine($"    public {rootClassName}(string url)");
            sb.AppendLine("    {");
            sb.AppendLine($"        Url = url;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public void Dispose()");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine();

            var paths = rootNode["paths"];
            AppendWebRequestsFromPaths(sb, paths);
            sb.AppendLine("}}");

            return sb.ToString();
        }

        private static string ConvertToCSharpType(JsonNode jsonNode)
        {
            var jsonType = string.Empty;
            if (jsonNode["type"] != null)
            {
                jsonType = jsonNode["type"].ToString();
            }
            else if (jsonNode["$ref"] != null)
            {
                var splited = jsonNode["$ref"]?.ToString().Split("/");
                return splited[splited.Length - 1];
            }
            else if (jsonNode["anyOf"] != null)
            {
                return ConvertToCSharpType(jsonNode["anyOf"][0]) + "?";
            }

            switch (jsonType)
            {
                case "string": return "string" + (jsonNode["nullable"]?.ToString() == "true" ? "?" : "");
                case "integer":
                    if (jsonNode["format"]?.ToString() == "int64")
                        return "Int64" + (jsonNode["nullable"]?.ToString() == "true" ? "?" : "");
                    return "int" + (jsonNode["nullable"]?.ToString() == "true" ? "?" : "");
                case "boolean": return "bool" + (jsonNode["nullable"]?.ToString() == "true" ? "?" : "");
                case "number": return "decimal" + (jsonNode["nullable"]?.ToString() == "true" ? "?" : "");

                default: return "object";
            }
        }

        private static void AppendWebRequestsFromPaths(StringBuilder sb, JsonNode pathsNode)
        {
            Dictionary<string, int> methodNameCounter = new Dictionary<string, int>();

            foreach (var pathItem in pathsNode as JsonObject)
            {
                var path = pathItem.Key;
                if (path.Contains("."))
                {
                    continue;
                }

                if (pathItem.Value is JsonObject methods)
                {
                    foreach (var method in methods)
                    {
                        var httpMethod = method.Key.ToUpper();

                        var methodName = GenerateMethodNameFromPathAndMethod(path, httpMethod);
                        var returnType = "string";

                        var parameterDefinitions = new StringBuilder();
                        var parameterUsages = new StringBuilder();

                        var parameters = method.Value["parameters"];
                        if (parameters != null)
                        {
                            var queryParameters = new List<string>();
                            var headerParameters = new List<string>();

                            var parameterTypeDict = new Dictionary<string, string>();
                            foreach (var parameter in parameters as JsonArray)
                            {
                                var parameterName = parameter["name"].ToString()
                                    .Replace(" ", "_")
                                    .Replace("-", "_");
                                var parameterType = ConvertToCSharpType(parameter["schema"]);
                                parameterDefinitions.Append($"{parameterType} {parameterName}, ");

                                parameterTypeDict[parameter["name"].ToString()] = parameterType.Replace("?", "");
                                switch (parameter["in"].ToString())
                                {
                                    case "query":
                                        queryParameters.Add($"{parameter["name"]}={{{parameterName}}}");
                                        break;
                                    case "header":
                                        headerParameters.Add(parameter["name"].ToString());
                                        break;
                                }
                            }

                            if (queryParameters.Any())
                            {
                                var queryString = string.Join("&", queryParameters);
                                parameterUsages.AppendLine($"            url += $\"?{queryString}\";");
                            }

                            parameterUsages.AppendLine("            request.uri = new Uri(url);");

                            foreach (var headerName in headerParameters)
                            {
                                if (parameterTypeDict.TryGetValue(headerName, out string type) && inValidEnumTypeList.Contains(type))
                                {
                                    var headerKey = MakeValidEnumName(headerName);
                                    parameterUsages.AppendLine($"            string headerValue = {headerKey}.ToString();");
                                    parameterUsages.AppendLine($"            if ({type}TypeConverter.InvalidEnumMapping.ContainsValue(headerValue))");
                                    parameterUsages.AppendLine($"            {{");
                                    parameterUsages.AppendLine($"                headerValue = {type}TypeConverter.InvalidEnumMapping.First(kvp => kvp.Value == headerValue).Key;");
                                    parameterUsages.AppendLine($"            }}");
                                    parameterUsages.AppendLine($"            request.SetRequestHeader(\"{headerName}\", headerValue);");
                                }
                                else
                                {
                                    parameterUsages.AppendLine($"            request.SetRequestHeader(\"{headerName}\", {MakeValidEnumName(headerName)}.ToString());");
                                }
                            }
                        }

                        var requestBody = method.Value["requestBody"];
                        if (requestBody != null)
                        {
                            var requestBodyType = "string";
                            if (requestBody["content"]["application/json"]["schema"]["$ref"] != null)
                            {
                                var schemaRef = requestBody["content"]["application/json"]["schema"]["$ref"].ToString();
                                var parts = schemaRef.Split('/');
                                requestBodyType = parts[parts.Length - 1];
                            }

                            parameterDefinitions.Append($"{requestBodyType} requestBody, ");
                            parameterUsages.AppendLine($"            var bodyString = System.Text.Json.JsonSerializer.Serialize(requestBody);");
                            parameterUsages.AppendLine($"            var jsonToSend = new UTF8Encoding().GetBytes(bodyString);");
                            parameterUsages.AppendLine($"            request.uploadHandler = new UploadHandlerRaw(jsonToSend);");
                            parameterUsages.AppendLine($"            request.uploadHandler.contentType = \"application/json\";");
                        }

                        var hasReturnType = false;
                        var responseSchema = method.Value["responses"]?["200"]?["content"]?["application/json"]?["schema"];
                        if (responseSchema != null)
                        {
                            if (responseSchema["$ref"] != null)
                            {
                                var schemaRef = responseSchema["$ref"].ToString();
                                var parts = schemaRef.Split('/');
                                returnType = parts[parts.Length - 1];
                                hasReturnType = true;
                            }

                            if (responseSchema["type"] != null &&
                                responseSchema["type"].ToString() == "array" &&
                                responseSchema["items"] != null)
                            {
                                string schemaRef;

                                if (responseSchema["items"]["$ref"] != null)
                                {
                                    schemaRef = responseSchema["items"]["$ref"].ToString();
                                    var parts = schemaRef.Split('/');
                                    returnType = parts[parts.Length - 1] + "[]";
                                }

                                if (responseSchema["items"]["type"] != null)
                                {
                                    schemaRef = responseSchema["items"]["type"].ToString();
                                    returnType = schemaRef + "[]";
                                }

                                hasReturnType = true;
                            }

                            var convert = ConvertToCSharpType(responseSchema);
                            if (convert != "object")
                            {
                                returnType = convert;
                                hasReturnType = true;
                            }
                        }

                        // 중복 체크를 위해 key 생성
                        string key = $"{methodName}-{parameterDefinitions}-{returnType}";

                        if (methodNameCounter.ContainsKey(key))
                        {
                            methodNameCounter[key]++;
                            methodName += methodNameCounter[key].ToString(); // 중복될 경우 숫자 추가
                        }
                        else
                        {
                            methodNameCounter[key] = 1; // 처음 발견된 경우
                        }

                        var callbacks = new StringBuilder();
                        var responseHandling = new StringBuilder();

                        JsonObject responses = method.Value["responses"] as JsonObject;
                        if (responses != null)
                        {
                            foreach (var response in responses)
                            {
                                var statusCode = response.Key;
                                var responseType = "string";

                                // 응답 설명 가져오기
                                var description = response.Value["description"]?.ToString() ?? "";
                                // 설명에서 유효한 파라미터 이름 생성
                                var descriptionPart = Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                                if (descriptionPart.Length > 30)
                                {
                                    descriptionPart = descriptionPart.Substring(0, 30);
                                }

                                var callbackName = $"on{statusCode}{descriptionPart}";

                                var responseSchema2 = response.Value["content"]?["application/json"]?["schema"];
                                if (responseSchema2 != null)
                                {
                                    if (responseSchema2["$ref"] != null)
                                    {
                                        var schemaRef = responseSchema2["$ref"].ToString();
                                        var parts = schemaRef.Split('/');
                                        responseType = parts[parts.Length - 1];
                                    }
                                    else if (responseSchema2["type"]?.ToString() == "array" && responseSchema2["items"] != null)
                                    {
                                        if (responseSchema2["items"]["$ref"] != null)
                                        {
                                            var schemaRef = responseSchema2["items"]["$ref"].ToString();
                                            var parts = schemaRef.Split('/');
                                            responseType = $"{parts[parts.Length - 1]}[]";
                                        }
                                        else if (responseSchema2["items"]["type"] != null)
                                        {
                                            responseType = $"{responseSchema2["items"]["type"]}[]";
                                        }
                                    }
                                    else
                                    {
                                        responseType = ConvertToCSharpType(responseSchema2);
                                    }
                                }

                                // 콜백 파라미터에 설명 주석 추가
                                callbacks.AppendLine($"        // {description}");
                                callbacks.AppendLine($"        Action<{responseType}> {callbackName} = null, ");

                                responseHandling.AppendLine($"        if (webRequest.responseCode == {statusCode}) // {description}");
                                responseHandling.AppendLine("        {");
                                responseHandling.AppendLine($"            if ({callbackName} != null)");
                                responseHandling.AppendLine("            {");
                                if (responseType != "string")
                                {
                                    responseHandling.AppendLine($"                {responseType} responseData;");
                                    responseHandling.AppendLine($"                try {{ responseData = System.Text.Json.JsonSerializer.Deserialize<{responseType}>(responseText); }}");
                                    responseHandling.AppendLine("                catch (JsonException ex) { onError(ex.Message + \" \\n\\nResponse Text: \" + responseText); return; }");
                                    responseHandling.AppendLine($"                {callbackName}(responseData);");
                                }
                                else
                                {
                                    responseHandling.AppendLine($"                {callbackName}(responseText);");
                                }
                                responseHandling.AppendLine("            }");
                                responseHandling.AppendLine("            else if (onError != null)");
                                responseHandling.AppendLine("            {");
                                responseHandling.AppendLine("                onError(responseText);");
                                responseHandling.AppendLine("            }");
                                responseHandling.AppendLine("            return;");
                                responseHandling.AppendLine("        }");
                            }
                        }

                        // Add method summary with operation description and parameters
                        var operationDescription = method.Value["description"]?.ToString();
                        var operationSummary = method.Value["summary"]?.ToString();

                        if (!string.IsNullOrEmpty(operationSummary) || !string.IsNullOrEmpty(operationDescription))
                        {
                            sb.AppendLine("    /// <summary>");
                            if (!string.IsNullOrEmpty(operationSummary))
                                sb.AppendLine($"    /// <para>{operationSummary.Replace("\r\n", "\r\n    /// ").Replace("\n", "\n    /// ")}</para>");
                            if (!string.IsNullOrEmpty(operationDescription))
                                sb.AppendLine($"    /// <para>{operationDescription.Replace("\r\n", "\r\n    /// ").Replace("\n", "\n    /// ")}</para>");
                            sb.AppendLine("    /// </summary>");
                        }

                        // Add parameter documentation
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters as JsonArray)
                            {
                                var paramName = parameter["name"].ToString();
                                var parameterDescription = parameter["description"]?.ToString();
                                if (!string.IsNullOrEmpty(parameterDescription))
                                {
                                    sb.AppendLine($"    /// <param name=\"{paramName}\">");
                                    sb.AppendLine($"    /// <para>{parameterDescription}</para>");
                                    sb.AppendLine("    /// </param>");
                                }
                            }
                        }

                        // Add response documentation
                        if (method.Value["responses"] is JsonObject responsesDoc)
                        {
                            foreach (var response in responsesDoc)
                            {
                                var statusCode = response.Key;
                                var responseDescription = response.Value["description"]?.ToString();
                                if (!string.IsNullOrEmpty(responseDescription))
                                {
                                    sb.AppendLine($"    /// <response code=\"{statusCode}\">");
                                    sb.AppendLine($"    /// <para>{responseDescription}</para>");
                                    sb.AppendLine("    /// </response>");
                                }
                            }
                        }

                        // 메서드 생성
                        sb.AppendLine($"    public async Task {methodName}(");
                        sb.AppendLine($"        {parameterDefinitions}");
                        sb.Append(callbacks);
                        sb.AppendLine("        Action<string> onError = null)");
                        sb.AppendLine("    {");
                        sb.AppendLine($"        string url = $\"{{Url}}{path}\";");
                        sb.AppendLine($"        using (var request = new UnityWebRequest(url, \"{httpMethod}\"))");
                        sb.AppendLine("        {");
                        sb.Append(parameterUsages);
                        sb.AppendLine($"            request.downloadHandler = new DownloadHandlerBuffer();");

                        sb.AppendLine($"            request.SetRequestHeader(\"accept\", \"application/json\");");
                        sb.AppendLine($"            request.SetRequestHeader(\"Content-Type\", \"application/json\");");
                        sb.AppendLine($"            request.timeout = {_timeOut};");
                        sb.AppendLine("            try");
                        sb.AppendLine("            {");
                        sb.AppendLine("                await request.SendWebRequest();");
                        sb.AppendLine($"                {methodName}ProcessResponse(request, " + string.Join(", ", GetCallbackNames(responses)) + ", onError);");
                        sb.AppendLine("            }");
                        sb.AppendLine("            catch (Exception ex)");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                {methodName}ProcessResponse(request, " + string.Join(", ", GetCallbackNames(responses)) + ", onError);");
                        sb.AppendLine("            }");
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine();

                        // ProcessResponse 메서드 생성
                        sb.AppendLine($"    private void {methodName}ProcessResponse(UnityWebRequest webRequest, {GetCallbackParameters(responses)}, Action<string> onError)");
                        sb.AppendLine("    {");
                        sb.AppendLine("        string responseText = webRequest.downloadHandler?.text ?? string.Empty;");
                        sb.Append(responseHandling);
                        sb.AppendLine("        if (onError != null)");
                        sb.AppendLine("        {");
                        sb.AppendLine("            onError(webRequest.error);");
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine();
                    }
                }
            }
        }

        private static string GenerateMethodNameFromPathAndMethod(string path, string httpMethod)
        {
            var sb = new StringBuilder();
            var methodName = httpMethod.ToLower();
            methodName = char.ToUpper(methodName[0]) + methodName.Substring(1);
            sb.Append(methodName);
            var segments = path.Split('/');
            foreach (var segment in segments)
            {
                if (!string.IsNullOrEmpty(segment))
                {
                    sb.Append(char.ToUpper(segment[0]) + segment.Substring(1));
                }
            }
            string result = sb.ToString().Replace("-", "").Replace("Api", string.Empty) + "Async";

            //{} 포함된 문자열 제거
            result = Regex.Replace(result, @"\{.*?\}", string.Empty);

            return result;
        }

        private static void RefreshGeneratedInfo()
        {
            if (Directory.Exists(_outputDir))
            {
                var filePaths = Directory.GetFiles(_outputDir, "*.cs", SearchOption.AllDirectories);
                if (filePaths.Length == _dataList.Count)
                {
                    return;
                }

                _dataList.Clear();
                foreach (var filePath in filePaths)
                {
                    var content = File.ReadAllText(filePath);
                    var pattern = @"//     Source URL: \S+";
                    var match = Regex.Match(content, pattern);
                    if (match.Success)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var sourceUrl = match.Value.Replace("//     Source URL: ", string.Empty);
                        _dataList.Add(new KeyValuePair<string, string>(fileName, sourceUrl));
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        // Helper method to check if an enum value name is valid
        // enum 값 이름이 유효한지 확인하는 헬퍼 메소드
        private static bool IsValidEnumName(string name)
        {
            // enum 이름은 문자 또는 밑줄로 시작하고 문자, 숫자, 밑줄만 포함해야 함
            var regex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");
            return regex.IsMatch(name);
        }

        // Helper method to check if a name is a C# reserved keyword
        // 이름이 C# 예약어인지 확인하는 헬퍼 메소드
        private static bool IsCSharpKeyword(string name)
        {
            // C# 예약어 목록
            var keywords = new HashSet<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
                "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
                "void", "volatile", "while"
            };
            return keywords.Contains(name);
        }

        // Helper method to make a string a valid enum name
        // 문자열을 유효한 enum 이름으로 만드는 헬퍼 메소드
        private static string MakeValidEnumName(string name)
        {
            // 점과 같은 유효하지 않은 문자를 밑줄로 대체
            var validName = Regex.Replace(name, @"[^a-zA-Z0-9_]+", "_");

            // 이름이 숫자로 시작하면 밑줄을 추가
            if (char.IsDigit(validName[0]))
            {
                validName = "_" + validName;
            }

            // 이름이 C# 예약어이면 밑줄을 추가
            if (IsCSharpKeyword(validName))
            {
                validName = "_" + validName;
            }

            return validName;
        }

        private static string GetCallbackParameters(JsonObject responses)
        {
            var parameters = new List<string>();
            foreach (var response in responses)
            {
                var statusCode = response.Key;
                var description = response.Value["description"]?.ToString() ?? "";
                var descriptionPart = Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                if (descriptionPart.Length > 30)
                {
                    descriptionPart = descriptionPart.Substring(0, 30);
                }

                var responseType = "string";
                var responseSchema = response.Value["content"]?["application/json"]?["schema"];
                if (responseSchema != null)
                {
                    if (responseSchema["$ref"] != null)
                    {
                        var schemaRef = responseSchema["$ref"].ToString();
                        var parts = schemaRef.Split('/');
                        responseType = parts[parts.Length - 1];
                    }
                    else if (responseSchema["type"]?.ToString() == "array" && responseSchema["items"] != null)
                    {
                        if (responseSchema["items"]["$ref"] != null)
                        {
                            var schemaRef = responseSchema["items"]["$ref"].ToString();
                            var parts = schemaRef.Split('/');
                            responseType = $"{parts[parts.Length - 1]}[]";
                        }
                        else if (responseSchema["items"]["type"] != null)
                        {
                            responseType = $"{responseSchema["items"]["type"]}[]";
                        }
                    }
                    else
                    {
                        responseType = ConvertToCSharpType(responseSchema);
                    }
                }

                parameters.Add($"Action<{responseType}> on{statusCode}{descriptionPart}");
            }
            return string.Join(", ", parameters);
        }

        private static string[] GetCallbackNames(JsonObject responses)
        {
            var names = new List<string>();
            foreach (var response in responses)
            {
                var statusCode = response.Key;
                var description = response.Value["description"]?.ToString() ?? "";
                var descriptionPart = Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                if (descriptionPart.Length > 30)
                {
                    descriptionPart = descriptionPart.Substring(0, 30);
                }
                names.Add($"on{statusCode}{descriptionPart}");
            }
            return names.ToArray();
        }
    }
}
